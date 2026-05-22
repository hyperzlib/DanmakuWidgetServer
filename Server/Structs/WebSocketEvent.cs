using BilibiliDM_PluginFramework;
using DanmakuWidgetServer.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Structs
{
    public class WebSocketEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("data")]
        [JsonConverter(typeof(RawJsonConverter))]
        public string Data { get; set; } = null;

        public class InitData
        {
            [JsonProperty("room_id")]
            public int? RoomId { get; set; }

            [JsonProperty("history_danmaku")]
            [JsonConverter(typeof(RawJsonConverter))]
            public string HistoryDanmaku { get; set; } = "[]";
        }
    }
}
