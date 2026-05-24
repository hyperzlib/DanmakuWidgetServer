using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Structs
{
    public class TemplateManifest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; } = null;

        [JsonProperty("preview_img", NullValueHandling = NullValueHandling.Ignore)]
        public string PreviewImg { get; set; } = null;

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public string Author { get; set; } = null;

        [JsonProperty("author_email", NullValueHandling = NullValueHandling.Ignore)]
        public string AuthorEmail { get; set; } = null;

        [JsonProperty("repository_url", NullValueHandling = NullValueHandling.Ignore)]
        public string RepositoryUrl { get; set; } = null;

        [JsonProperty("website_url", NullValueHandling = NullValueHandling.Ignore)]
        public string WebsiteUrl { get; set; } = null;

        [JsonProperty("configure_file", NullValueHandling = NullValueHandling.Ignore)]
        public string ConfigureFile { get; set; } = null;

        [JsonProperty("template_file", NullValueHandling = NullValueHandling.Ignore)]
        public string TemplateFile { get; set; } = null;

        [JsonProperty("configure_url")]
        public string ConfigureUrl { get; set; } = null;

        [JsonProperty("template_url")]
        public string TemplateUrl { get; set; } = string.Empty;
    }
}
