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

        [Route(HttpVerbs.Get, "/skins")]
        public async Task GetSkinList()
        {
            // 遍历皮肤文件夹，获取所有皮肤的名称列表
            try
            {
                var skinList = new List<SkinManifest>();
                var warnings = new List<string>();
                foreach (var skinDir in Directory.GetDirectories(config.TemplateFolder))
                {
                    var skinManifestPath = Path.Combine(skinDir, "skin.json");
                    if (File.Exists(skinManifestPath))
                    {
                        var skinName = Path.GetFileName(skinDir);
                        try
                        {
                            using (var reader = new StreamReader(skinManifestPath))
                            {
                                var manifestContent = await reader.ReadToEndAsync();
                                var skinManifest = JsonConvert.DeserializeObject<SkinManifest>(manifestContent);

                                skinManifest.SkinUrl = $"/{Path.GetFileName(skinDir)}/";
                                if (!string.IsNullOrEmpty(skinManifest.PreviewImg))
                                {
                                    skinManifest.PreviewImg = skinManifest.SkinUrl + skinManifest.PreviewImg;
                                }

                                skinList.Add(skinManifest);
                            }
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"无法加载皮肤 [{Path.GetFileName(skinDir)}]：{ex.Message}");
                        }
                    }
                    else
                    {
                        var skinIndexFile = Path.Combine(skinDir, "index.html");
                        if (File.Exists(skinIndexFile))
                        {
                            var skinManifest = new SkinManifest
                            {
                                Name = Path.GetFileName(skinDir),
                                SkinUrl = $"/{Path.GetFileName(skinDir)}/"
                            };
                            skinList.Add(skinManifest);
                        }
                    }
                }

                var responseData = new
                {
                    status = 200,
                    message = "获取皮肤列表成功",
                    data = skinList,
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
