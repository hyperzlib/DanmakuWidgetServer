using BilibiliDM_PluginFramework;
using DanmakuWidgetServer.Server.Controllers;
using DanmakuWidgetServer.Server.Modules;
using DanmakuWidgetServer.Server.Structs;
using DanmakuWidgetServer.Server.WebSocket;
using EmbedIO;
using EmbedIO.WebApi;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server
{
    internal class WidgetServer : IDisposable
    {
        private WebServer webServer = null;
        private bool disposedValue;

        private TaskCompletionSource<bool> startedSource = new TaskCompletionSource<bool>();
        private TaskCompletionSource<bool> stoppedSource = new TaskCompletionSource<bool>();

        internal DanmakuWebSocketServer danmakuWsServer = null;

        internal event Action<IWebSocketContext> OnDanmakuWebSocketConnected;

        public int ServerPort { get; private set; } = 0;

        public event Action Started;

        public async Task StartAsync(PluginConfig config)
        {
            ServerPort = config.HttpPort;
            var url = $"http://*:{config.HttpPort}";
            if (config.AllowLan)
            {
                url = $"http://*:{config.HttpPort}";
            }

            danmakuWsServer = new DanmakuWebSocketServer("/ws/danmaku");
            danmakuWsServer.OnSocketConnected += DanmakuWsServer_OnSocketConnected;

            webServer = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ACLModule(config.AllowLan))
                .WithModule(new ProxyModule("/proxy"))
                .WithModule(danmakuWsServer)
                .WithWebApi("/api", m => m.WithController(() => new ApiController(config)))
                .WithStaticFolder("/", config.TemplateFolder, false, (fileModule) =>
                {

                });

            webServer.StateChanged += WebServer_StateChanged;

            startedSource = new TaskCompletionSource<bool>();
            webServer.Start();
            await startedSource.Task;
        }

        private void DanmakuWsServer_OnSocketConnected(IWebSocketContext socket)
        {
            OnDanmakuWebSocketConnected?.Invoke(socket);
        }

        private void WebServer_StateChanged(object sender, WebServerStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case WebServerState.Listening:
                    Console.WriteLine("[WidgetServer] started at http://localhost:{0}", ServerPort);
                    startedSource.TrySetResult(true);
                    Started?.Invoke();
                    break;
                case WebServerState.Stopped:
                    Console.WriteLine("[WidgetServer] stopped.");
                    stoppedSource.TrySetResult(true);
                    break;
            }
        }

        internal async Task BroadcastDanmaku(DanmakuModel danmaku)
        {
            var danmakuJson = JsonConvert.SerializeObject(danmaku.RawDataJToken);
            var message = new WebSocketEvent()
            {
                Type = "danmaku",
                Data = danmakuJson
            };

            await danmakuWsServer.BroadcastJsonAsync(message);
        }

        internal async Task BroadcastOtherLiveEvent(DanmakuModel liveEvent)
        {
            var liveEventJson = JsonConvert.SerializeObject(liveEvent.RawDataJToken);
            var message = new WebSocketEvent()
            {
                Type = "other_live_event",
                Data = liveEventJson
            };

            await danmakuWsServer.BroadcastJsonAsync(message);
        }

        internal async Task BroadcastConnectedEvent(int? roomId)
        {
            var dataObj = new
            {
                room_id = roomId,
            };
            var dataJson = JsonConvert.SerializeObject(dataObj);
            var message = new WebSocketEvent()
            {
                Type = "connected",
                Data = dataJson
            };

            await danmakuWsServer.BroadcastJsonAsync(message);
        }

        internal async Task BroadcastDisconnectedEvent(Exception error)
        {
            var dataObj = new
            {
                error = error?.Message,
                trace = error?.StackTrace
            };
            var dataJson = JsonConvert.SerializeObject(dataObj);
            var message = new WebSocketEvent()
            {
                Type = "disconnected",
                Data = dataJson
            };
            await danmakuWsServer.BroadcastJsonAsync(message);
        }

        internal async Task SendInitEvent(IWebSocketContext socket, int? roomId, List<DanmakuModel> danmakuList)
        {
            var historyDanmakuJsonList = danmakuList.Select(d => d.RawDataJToken).ToList();
            var historyDanmakuJson = JsonConvert.SerializeObject(historyDanmakuJsonList);

            var initData = new WebSocketEvent.InitData()
            {
                RoomId = roomId,
                HistoryDanmaku = historyDanmakuJson,
            };
            var initDataJson = JsonConvert.SerializeObject(initData);

            var message = new WebSocketEvent()
            {
                Type = "init",
                Data = initDataJson
            };
            await danmakuWsServer.SendJsonAsync(socket, message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    webServer?.Dispose();
                    webServer = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
