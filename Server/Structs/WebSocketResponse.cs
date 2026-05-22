using Newtonsoft.Json;
using DanmakuWidgetServer.Converters;

namespace DanmakuWidgetServer.Server.Structs
{
    public class WebSocketResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; } = 200;

        [JsonProperty("message")]
        public string Message { get; set; } = "ok";

        [JsonProperty("event")]
        public string EventName { get; set; }

        [JsonProperty("mid")]
        public string ResponseMsgId { get; set; }

        [JsonProperty("data")]
        [JsonConverter(typeof(RawJsonConverter))]
        public string Data { get; set; } = "null";
    }
}
