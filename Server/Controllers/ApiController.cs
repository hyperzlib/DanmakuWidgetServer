using DanmakuWidgetServer.Structs;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanmakuWidgetServer.Server.Controllers
{
    internal class ApiController : WebApiController
    {
        private PluginConfig config;

        public ApiController(PluginConfig config) : base()
        {
            this.config = config;
        }

        [Route(HttpVerbs.Get, "/templates")]
        public async Task GetTemplateList()
        {
            // 遍历模板文件夹，获取所有模板的信息
            try
            {
                var tplList = new List<TemplateManifest>();
                var warnings = new List<string>();
                foreach (var tplDir in Directory.GetDirectories(config.TemplateFolder))
                {
                    var tplManifestPath = Path.Combine(tplDir, "template.json");
                    if (File.Exists(tplManifestPath))
                    {
                        var tplName = Path.GetFileName(tplDir);
                        try
                        {
                            using (var reader = new StreamReader(tplManifestPath))
                            {
                                var manifestContent = await reader.ReadToEndAsync();
                                var tplManifest = JsonConvert.DeserializeObject<TemplateManifest>(manifestContent);

                                tplManifest.TemplateUrl = $"/{Path.GetFileName(tplDir)}/";
                                if (!string.IsNullOrEmpty(tplManifest.PreviewImg))
                                {
                                    tplManifest.PreviewImg = tplManifest.TemplateUrl + tplManifest.PreviewImg;
                                }

                                tplList.Add(tplManifest);
                            }
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"无法加载模板信息 [{Path.GetFileName(tplDir)}]：{ex.Message}");
                        }
                    }
                    else
                    {
                        var tplIndexFile = Path.Combine(tplDir, "index.html");
                        if (File.Exists(tplIndexFile))
                        {
                            var tplManifest = new TemplateManifest
                            {
                                Name = Path.GetFileName(tplDir),
                                TemplateUrl = $"/{Path.GetFileName(tplDir)}/"
                            };
                            tplList.Add(tplManifest);
                        }
                    }
                }

                var responseData = new
                {
                    status = 200,
                    message = "获取皮肤列表成功",
                    data = tplList,
                    warnings = warnings
                };
                var responseJson = JsonConvert.SerializeObject(responseData);
                await HttpContext.SendStringAsync(responseJson, "application/json", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                var responseData = new
                {
                    status = 500,
                    message = "获取皮肤列表失败：" + ex.Message,
                    trace = ex.StackTrace,
                };
                var responseJson = JsonConvert.SerializeObject(responseData);
                await HttpContext.SendStringAsync(responseJson, "application/json", Encoding.UTF8);
            }
        }
    }
}
