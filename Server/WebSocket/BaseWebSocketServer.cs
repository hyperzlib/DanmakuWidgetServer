using DanmakuWidgetServer.Server.Structs;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.WebSocket
{
    internal abstract class BaseWebSocketServer : WebSocketModule
    {
        protected readonly ConcurrentDictionary<string, MemoryStream> messageBuffers = new ConcurrentDictionary<string, MemoryStream>();
        protected int maxMessageSize = 20 * 1024 * 1024; // 20 MB

        public event Action<IWebSocketContext> OnSocketConnected;

        public BaseWebSocketServer(string urlPath, int maxMessageSize = 20 * 1024 * 1024)
            : base(urlPath, true)
        {
            this.maxMessageSize = maxMessageSize;
        }

        protected async override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            await base.OnClientConnectedAsync(context);
            OnSocketConnected?.Invoke(context);
        }

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            if (messageBuffers.TryRemove(context.Id, out var buffer))
            {
                try
                {
                    buffer.Dispose();
                }
                catch { }
            }
            return Task.CompletedTask;
        }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
        {
            // 处理 Close 帧，避免把 Close 当成业务消息
            if (result.MessageType == ((int)WebSocketMessageType.Close))
            {
                if (messageBuffers.TryRemove(context.Id, out var toDispose))
                {
                    try { toDispose.Dispose(); } catch { }
                }
                return;
            }

            try
            {
                if (result.EndOfMessage)
                {
                    // 完整消息：如果之前有分片缓存则合并，否则直接使用当前 buffer 的有效长度
                    if (messageBuffers.TryRemove(context.Id, out var msgBuffer))
                    {
                        // 写入最后一段
                        msgBuffer.Write(buffer, 0, result.Count);

                        // 防护：总长度限制
                        if (msgBuffer.Length > maxMessageSize)
                        {
                            Console.WriteLine("[WebSocketStateServer] 消息超过最大允许长度，已丢弃。ContextId: {Id}", context.Id);
                            try { msgBuffer.Dispose(); } catch { }
                            await SendJsonAsync(context, new WebSocketResponse()
                            {
                                Status = 413,
                                EventName = "error",
                                Message = "消息过大",
                            });
                            return;
                        }

                        var fullMessage = msgBuffer.ToArray();
                        try { msgBuffer.Dispose(); } catch { }
                        await OnFullMessageReceivedAsync(context, fullMessage);
                    }
                    else
                    {
                        // 直接使用当前 buffer 的有效字节数（避免包含多余尾部）
                        var final = buffer.Take(result.Count).ToArray();

                        if (final.Length > maxMessageSize)
                        {
                            Console.WriteLine("[WebSocketStateServer] 消息超过最大允许长度，已丢弃。ContextId: {Id}", context.Id);
                            await SendJsonAsync(context, new WebSocketResponse()
                            {
                                Status = 413,
                                EventName = "error",
                                Message = "消息过大",
                            });
                            return;
                        }

                        await OnFullMessageReceivedAsync(context, final);
                    }
                }
                else
                {
                    // 分片消息：附加到或创建流
                    var msgBuffer = messageBuffers.GetOrAdd(context.Id, _ => new MemoryStream());

                    // 防护：如果写入后超过上限，丢弃并回复错误
                    if (msgBuffer.Length + result.Count > maxMessageSize)
                    {
                        // 尝试移除并释放
                        messageBuffers.TryRemove(context.Id, out var removed);
                        try { removed?.Dispose(); } catch { }

                        Console.WriteLine("[WebSocketStateServer] 分片消息累计超过最大允许长度，已丢弃。ContextId: {Id}", context.Id);
                        await SendJsonAsync(context, new WebSocketResponse()
                        {
                            Status = 413,
                            EventName = "error",
                            Message = "消息过大",
                        });
                        return;
                    }

                    msgBuffer.Write(buffer, 0, result.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WebSocketStateServer] 处理接收消息时发生异常: {0}", ex.Message);
                Console.WriteLine(ex);
                // 尝试清理该连接的缓冲
                if (messageBuffers.TryRemove(context.Id, out var buf))
                {
                    try { buf.Dispose(); } catch { }
                }
                await SendJsonAsync(context, new WebSocketResponse()
                {
                    Status = 500,
                    EventName = "error",
                    Message = "服务器内部错误",
                });
            }
        }

        protected virtual async Task OnFullMessageReceivedAsync(IWebSocketContext context, byte[] buffer)
        {
            var text = Encoding.UTF8.GetString(buffer);
            try
            {
                var request = JsonConvert.DeserializeObject<WebSocketRequest>(text) ??
                    throw new Exception("反序列化结果为空");
                await HandleRequest(context, request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocketStateServer] 接收消息反序列化失败: {ex}");
                await SendJsonAsync(context, new WebSocketResponse()
                {
                    Status = 400,
                    EventName = "error",
                    Message = "消息格式错误，无法解析",
                });
            }
        }

        protected virtual Task HandleRequest(IWebSocketContext context, WebSocketRequest request)
        {
            return Task.CompletedTask;
        }

        public async Task SendJsonAsync<T>(IWebSocketContext context, T data)
        {
            var json = JsonConvert.SerializeObject(data);
            await SendAsync(context, json);
        }

        public async Task BroadcastJsonAsync<T>(T data)
        {
            var json = JsonConvert.SerializeObject(data);
            await BroadcastAsync(json);
        }
    }
}
