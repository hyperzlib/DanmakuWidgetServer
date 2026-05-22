using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer
{
    public class PluginConfig
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("http_port")]
        public int HttpPort { get; set; } = 2365;

        [JsonProperty("template_folder")]
        public string TemplateFolder { get; set; } = string.Empty;

        [JsonProperty("allow_lan")]
        public bool AllowLan { get; set; } = false;

        public static PluginConfig Load(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return new PluginConfig();
            }
            try
            {
                var configContent = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<PluginConfig>(configContent);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"加载配置失败：{ex.Message}");
                Console.Error.WriteLine(ex);
                return new PluginConfig();
            }
        }

        public void Save(string path)
        {
            try
            {
                var configContent = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, configContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"保存配置失败：{ex.Message}");
                Console.Error.WriteLine(ex);
            }
        }
    }
}
