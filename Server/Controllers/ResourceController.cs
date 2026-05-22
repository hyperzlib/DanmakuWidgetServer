using DanmakuWidgetServer.Server.Utils;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Controllers
{
    internal class ResourceController : WebApiController
    {
        private static string TemplatePath = "";

        [Route(HttpVerbs.Get, "/{resourcePath}")]
        public async Task GetResource(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                HttpContext.Response.StatusCode = 404;
                await HttpContext.SendStringAsync("File Not Found", "text/plain", Encoding.UTF8);
                return;
            }

            try
            {
                var absolutePath = Path.Combine(TemplatePath, resourcePath);
                var fileStat = new FileInfo(absolutePath);
                if (fileStat.Exists)
                {
                    if (fileStat.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        await ServerUtils.SendDirectoryListing(HttpContext, absolutePath, resourcePath);
                    }
                    else
                    {
                        await ServerUtils.SendStaticFile(HttpContext, resourcePath);
                    }
                }
                else
                {
                    HttpContext.Response.StatusCode = 403;
                    await HttpContext.SendStringAsync("Access denied to list directory", "text/plain", Encoding.UTF8);
                }
            }
            catch (FileNotFoundException)
            {
                HttpContext.Response.StatusCode = 404;
                await HttpContext.SendStringAsync("File not found", "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                await HttpContext.SendStringAsync($"Internal Server Error: {ex.Message}", "text/plain", Encoding.UTF8);
            }
        }

        public static void SetTemplatePath(string path)
        {
            TemplatePath = path;
        }
    }
}
