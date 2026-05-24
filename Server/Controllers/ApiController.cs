using DanmakuWidgetServer.Server.Structs;
using DanmakuWidgetServer.Server.Utils;
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

                                var tplBaseUrl = $"/{Path.GetFileName(tplDir)}/";
                                if (!string.IsNullOrEmpty(tplManifest.TemplateFile))
                                {
                                    tplManifest.TemplateUrl = tplBaseUrl + tplManifest.TemplateFile.TrimStart('/');
                                    tplManifest.TemplateFile = tplManifestPath;
                                }
                                else
                                {
                                    tplManifest.TemplateUrl = tplBaseUrl + "index.html";
                                }
                                if (!string.IsNullOrEmpty(tplManifest.PreviewImg))
                                {
                                    tplManifest.PreviewImg = tplBaseUrl + tplManifest.PreviewImg.TrimStart('/');
                                }
                                if (!string.IsNullOrEmpty(tplManifest.ConfigureFile))
                                {
                                    tplManifest.ConfigureUrl = tplBaseUrl + tplManifest.ConfigureFile.TrimStart('/') +
                                        "?baseUrl=" + Uri.EscapeDataString(tplBaseUrl);
                                    tplManifest.ConfigureFile = null;
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

                // 获取IP地址列表
                var ipAddrList = NetUtils.GetAllIPv4Addresses();
                ipAddrList = NetUtils.SortByLocalAddressPriority(ipAddrList);
                var port = config.HttpPort;
                var protocol = "http"; // 暂时仅支持http协议
                var baseUrlList = ipAddrList.Select(ip =>
                    ip == "127.0.0.1" ?
                    $"{protocol}://localhost:{port}" :
                    $"{protocol}://{ip}:{port}").ToList();

                var responseData = new HttpApiResponse.ListTemplateResData()
                {
                    BaseUrlList = baseUrlList,
                    Templates = tplList
                };
                var responseDataJson = JsonConvert.SerializeObject(responseData);

                var response = new HttpApiResponse()
                {
                    Status = 200,
                    Message = "ok",
                    Warnings = warnings,
                    Data = responseDataJson
                };
                var responseJson = JsonConvert.SerializeObject(response);

                await HttpContext.SendStringAsync(responseJson, "application/json", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                var responseData = new
                {
                    status = 500,
                    message = "获取模板列表失败：" + ex.Message,
                    error_trace = ex.StackTrace,
                };
                var responseJson = JsonConvert.SerializeObject(responseData);
                await HttpContext.SendStringAsync(responseJson, "application/json", Encoding.UTF8);
            }
        }
    }
}
