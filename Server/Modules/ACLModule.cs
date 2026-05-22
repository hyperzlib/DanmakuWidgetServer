using EmbedIO;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Modules
{
    internal class ACLModule : WebModuleBase
    {
        private readonly bool allowLan;

        public ACLModule(bool allowLan)
            : base("/")
        {
            this.allowLan = allowLan;
        }

        public override bool IsFinalHandler => false;

        protected override Task OnRequestAsync(IHttpContext context)
        {
            if (allowLan || IsAllowedRequest(context))
            {
                return Task.CompletedTask;
            }

            context.Response.StatusCode = 403;
            context.SetHandled();
            return context.SendStringAsync("Forbidden", "text/plain", Encoding.UTF8);
        }

        private static bool IsAllowedRequest(IHttpContext context)
        {
            var remoteAddress = context.Request.RemoteEndPoint?.Address;
            if (remoteAddress == null)
            {
                return false;
            }

            if (IPAddress.IsLoopback(remoteAddress))
            {
                return true;
            }

            if (remoteAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                remoteAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var bytes = remoteAddress.GetAddressBytes();
                if (bytes.Length == 4 && bytes[0] == 127)
                {
                    return true;
                }
                else if (bytes.Length == 16)
                {
                    // ::ff:127.0.0.1
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        if (i < 10 && bytes[i] != 0) return false;
                        if (i >= 10 && i < 12 && bytes[i] != 0xff) return false;
                        if (i == 12 && bytes[i] != 127) return false;
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
