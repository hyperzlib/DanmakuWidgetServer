using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Structs
{
    public class SkinManifest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = null;

        [JsonProperty("version")]
        public string Version { get; set; } = null;

        [JsonProperty("preview_img")]
        public string PreviewImg { get; set; } = null;

        [JsonProperty("author")]
        public string Author { get; set; } = null;

        [JsonProperty("author_email")]
        public string AuthorEmail { get; set; } = null;

        [JsonProperty("repository_url")]
        public string RepositoryUrl { get; set; } = null;

        [JsonProperty("website_url")]
        public string WebsiteUrl { get; set; } = null;

        [JsonProperty("skin_url")]
        public string SkinUrl { get; set; } = string.Empty;
    }
}
