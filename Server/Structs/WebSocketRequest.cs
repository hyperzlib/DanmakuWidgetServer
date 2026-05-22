using Newtonsoft.Json;
using DanmakuWidgetServer.Converters;

namespace DanmakuWidgetServer.Server.Structs
{
    public class WebSocketRequest
    {
        [JsonProperty("mid")]
        public string MsgId { get; set; }

        [JsonProperty("action")]
        public string ActionName { get; set; }

        [JsonProperty("data")]
        [JsonConverter(typeof(RawJsonConverter))]
        public string Data { get; set; } = "null";
    }
}
