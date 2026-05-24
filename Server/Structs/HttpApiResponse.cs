using DanmakuWidgetServer.Converters;
using DanmakuWidgetServer.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Structs
{
    internal class HttpApiResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; } = 200;

        [JsonProperty("message")]
        public string Message { get; set; } = "ok";

        [JsonProperty("warnings")]
        public IEnumerable<string> Warnings { get; set; }

        [JsonProperty("error_trace", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorTrace { get; set; } = null;

        [JsonProperty("data")]
        [JsonConverter(typeof(RawJsonConverter))]
        public string Data { get; set; } = "null";

        internal class ListTemplateResData
        {
            [JsonProperty("base_url_list")]
            public IEnumerable<string> BaseUrlList { get; set; } = Array.Empty<string>();

            [JsonProperty("templates")]
            public IEnumerable<TemplateManifest> Templates { get; set; } = Array.Empty<TemplateManifest>();
        }
    }
}
