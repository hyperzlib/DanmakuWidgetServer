using EmbedIO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Modules
{
    internal class ProxyModule : WebModuleBase
    {
        private static readonly HttpClient HttpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        });

        private readonly string routeBase;

        public ProxyModule(string routeBase)
            : base(routeBase)
        {
            if (string.IsNullOrWhiteSpace(routeBase))
            {
                throw new ArgumentException("routeBase cannot be null or whitespace.", nameof(routeBase));
            }

            this.routeBase = routeBase.TrimEnd('/');
        }

        public override bool IsFinalHandler => true;

        protected override async Task OnRequestAsync(IHttpContext context)
        {
            var method = context.Request.HttpMethod;

            if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                AddCorsHeaders(context);
                context.Response.StatusCode = 204;
                context.SetHandled();
                return;
            }

            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 405;
                await context.SendStringAsync("Method Not Allowed, Proxy server only supports GET and OPTIONS requests", "text/plain", Encoding.UTF8);
                return;
            }

            AddCorsHeaders(context);

            if (!TryGetTargetUrl(context, out var targetUrl) || !TryBuildTargetUri(targetUrl, out var uri))
            {
                context.Response.StatusCode = 400;
                context.SetHandled();
                await context.SendStringAsync("Invalid target URL", "text/plain", Encoding.UTF8);
                return;
            }

            using (var proxyRequest = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                ForwardRequestHeaders(context, proxyRequest);

                using (var proxyResponse = await HttpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead))
                {
                    var response = context.Response;
                    response.StatusCode = (int)proxyResponse.StatusCode;

                    CopyResponseHeaders(context, proxyResponse);

                    if (proxyResponse.Content?.Headers.ContentType != null)
                    {
                        response.ContentType = proxyResponse.Content.Headers.ContentType.ToString();
                    }

                    if (proxyResponse.Content?.Headers.ContentLength != null)
                    {
                        response.ContentLength64 = proxyResponse.Content.Headers.ContentLength.Value;
                    }

                    using (var body = await proxyResponse.Content.ReadAsStreamAsync())
                    {
                        await body.CopyToAsync(response.OutputStream);
                        await response.OutputStream.FlushAsync();
                    }
                }
            }

            context.SetHandled();
        }

        private void AddCorsHeaders(IHttpContext context)
        {
            var responseHeaders = context.Response.Headers;
            responseHeaders["Access-Control-Allow-Origin"] = "*";
            responseHeaders["Access-Control-Allow-Methods"] = "GET, OPTIONS";
            responseHeaders["Access-Control-Allow-Headers"] = "*";
            responseHeaders["Access-Control-Expose-Headers"] = "Content-Encoding, Content-Length, Content-Range, Content-Type, ETag, Last-Modified";
        }

        private bool TryGetTargetUrl(IHttpContext context, out string targetUrl)
        {
            targetUrl = null;
            var requestUrl = context.Request.Url;
            if (requestUrl == null)
            {
                return false;
            }

            var pathAndQuery = requestUrl.PathAndQuery;
            if (string.IsNullOrEmpty(pathAndQuery))
            {
                return false;
            }

            var routePrefix = routeBase + "/";
            if (!pathAndQuery.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            targetUrl = pathAndQuery.Substring(routePrefix.Length);
            return !string.IsNullOrWhiteSpace(targetUrl);
        }

        private bool TryBuildTargetUri(string targetUrl, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return false;
            }

            var decodedUrl = Uri.UnescapeDataString(targetUrl);
            if (!Uri.TryCreate(decodedUrl, UriKind.Absolute, out uri))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private void ForwardRequestHeaders(IHttpContext context, HttpRequestMessage proxyRequest)
        {
            foreach (var headerKey in context.Request.Headers.AllKeys)
            {
                if (string.IsNullOrEmpty(headerKey))
                {
                    continue;
                }

                if (string.Equals(headerKey, "Host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerKey, "Origin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerKey, "Referer", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var headerValue = context.Request.Headers[headerKey];
                if (string.IsNullOrEmpty(headerValue))
                {
                    continue;
                }

                proxyRequest.Headers.TryAddWithoutValidation(headerKey, headerValue);
            }
        }

        private void CopyResponseHeaders(IHttpContext context, HttpResponseMessage proxyResponse)
        {
            var excludedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Transfer-Encoding",
                "Connection",
                "Keep-Alive",
                "Proxy-Authenticate",
                "Proxy-Authorization",
                "TE",
                "Trailer",
                "Upgrade",
                "Content-Length"
            };

            foreach (var header in proxyResponse.Headers)
            {
                if (excludedHeaders.Contains(header.Key))
                {
                    continue;
                }

                context.Response.Headers[header.Key] = string.Join(",", header.Value);
            }

            if (proxyResponse.Content == null)
            {
                return;
            }

            foreach (var header in proxyResponse.Content.Headers)
            {
                if (excludedHeaders.Contains(header.Key) || string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                context.Response.Headers[header.Key] = string.Join(",", header.Value);
            }
        }
    }
}
