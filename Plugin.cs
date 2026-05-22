using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using BilibiliDM_PluginFramework;
using DanmakuWidgetServer.Server;

namespace DanmakuWidgetServer
{
    public class Plugin : DMPlugin
    {
        public static string PluginDataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            @"弹幕姬\plugins\DanmakuWidgetServer\");

        internal object syncRoot = new object();
        internal WidgetServer widgetServer = null;
        internal PluginConfig config = null;

        private static bool assemblyResolverRegistered = false;

        internal static int MaxHistoryDanmaku = 100;
        internal List<DanmakuModel> historyDanmakuList = new List<DanmakuModel>();

        public Plugin()
        {
            RegisterAssemblyResolver();

            this.PluginName = "Web弹幕机";
            this.PluginDesc = "让弹幕姬提供 Web 弹幕机服务器，供 OBS 等软件使用。";
            this.PluginAuth = "Hyperzlib";
            this.PluginCont = "hyperzlib@outlook.com";
            this.PluginVer = "0.0.1";

            Connected += Plugin_Connected;
            Disconnected += Plugin_Disconnected;
            ReceivedDanmaku += Plugin_ReceivedDanmaku;

            // 创建数据文件夹
            if (PluginDataPath != null && !Directory.Exists(PluginDataPath))
            {
                Directory.CreateDirectory(PluginDataPath);
            }
        }

        private void Plugin_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (e.Danmaku == null) return;

            var danmaku = e.Danmaku;

            // 只保存重要消息（评论、礼物、醒目留言、警告），其他消息如进入房间、关注等不保存到历史列表中
            bool isImportantMsg = danmaku.MsgType == MsgTypeEnum.Comment ||
                                  danmaku.MsgType == MsgTypeEnum.GiftSend ||
                                  danmaku.MsgType == MsgTypeEnum.SuperChat ||
                                  danmaku.MsgType == MsgTypeEnum.Warning;

            if (isImportantMsg)
            {
                PushHistoryDanmaku(danmaku);
            }

            Task.Run(async () =>
            {
                try
                {
                    if (isImportantMsg)
                    {
                        widgetServer?.BroadcastDanmaku(danmaku);
                    }
                    else
                    {
                        widgetServer?.BroadcastOtherLiveEvent(danmaku);
                    }
                }
                catch (Exception ex)
                {
                    Log($"发送弹幕到Web弹幕机服务器失败：{ex.Message}");
                }
            });
        }

        private void Plugin_Connected(object sender, ConnectedEvtArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    widgetServer?.BroadcastConnectedEvent(e?.roomid);
                }
                catch (Exception ex)
                {
                    Log($"发送弹幕到Web弹幕机服务器失败：{ex.Message}");
                }
            });
        }

        private void Plugin_Disconnected(object sender, DisconnectEvtArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    widgetServer?.BroadcastDisconnectedEvent(e?.Error);
                }
                catch (Exception ex)
                {
                    Log($"发送弹幕到Web弹幕机服务器失败：{ex.Message}");
                }
            });
        }

        private void PushHistoryDanmaku(DanmakuModel model)
        {
            historyDanmakuList.Add(model);

            if (MaxHistoryDanmaku < 0) return;

            while (historyDanmakuList.Count > MaxHistoryDanmaku)
            {
                historyDanmakuList.RemoveAt(0);
            }
        }

        private void RegisterAssemblyResolver()
        {
            if (assemblyResolverRegistered)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var assemblyName = new AssemblyName(args.Name).Name + ".dll";
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[]
                {
                    Path.Combine(baseDirectory, "libs", assemblyName),
                    Path.Combine(PluginDataPath, "libs", assemblyName)
                };

                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        return Assembly.LoadFrom(candidate);
                    }
                }

                return null;
            };

            assemblyResolverRegistered = true;
        }

        public override void Inited()
        {
            var configFile = Path.Combine(PluginDataPath, "config.json");
            if (File.Exists(configFile))
            {
                try
                {
                    config = PluginConfig.Load(configFile);
                }
                catch (Exception ex)
                {
                    Log($"加载配置文件失败：{ex.Message}");
                    Log("将使用默认配置。");
                }
            }
            else
            {
                Log("正在初始化配置文件");
            }

            if (config == null)
            {
                var tplPath = Path.Combine(PluginDataPath, "templates");
                if (!Directory.Exists(tplPath))
                {
                    Directory.CreateDirectory(tplPath);
                    Log($"已创建模板目录：{tplPath}");
                }

                config = new PluginConfig
                {
                    TemplateFolder = tplPath
                };

                config.Save(configFile);
            }

            if (File.Exists(Path.Combine(PluginDataPath, ".enabled")))
            {
                Log("已启用");
                Start();
            }
        }

        public override void Admin()
        {
            base.Admin();

            if (!this.Status)
            {
                MessageBox.Show("插件未启动，请先启动插件！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var settingsWindow = new SettingsWindow(config, this);

                settingsWindow.Show();
            });
        }
            

        public override void Start()
        {
            base.Start();

            if (config == null)
            {
                Log("配置未加载，无法启动Web弹幕机服务器！");
                Stop();
                return;
            }

            Task.Run(() =>
            {
                StartServer();
            });
        }

        public override void Stop()
        {
            Task.Run(() =>
            {
                lock (widgetServer)
                {
                    StopServer();
                }
            });

            historyDanmakuList.Clear();

            var enabledFile = Path.Combine(PluginDataPath, ".enabled");
            if (File.Exists(enabledFile))
            {
                File.Delete(enabledFile);
            }

            base.Stop();
        }

        public void StopServer()
        {
            lock (syncRoot)
            {
                if (widgetServer != null)
                {
                    Log("正在停止Web弹幕机服务器...");
                    widgetServer.Dispose();
                    widgetServer = null;
                    Log("服务器已停止");
                }
            }
        }

        public void StartServer()
        {
            lock (syncRoot)
            {
                widgetServer = new WidgetServer();
                widgetServer.OnDanmakuWebSocketConnected += WidgetServer_OnDanmakuWebSocketConnected;
            }

            var enabledFile = Path.Combine(PluginDataPath, ".enabled");
            if (!File.Exists(enabledFile))
            {
                File.Create(enabledFile).Close();
            }

            Task.Run(async () =>
            {
                Log($"正在端口 {config.HttpPort} 上启动Web弹幕机服务器...");
                try
                {
                    await widgetServer.StartAsync(config);
                    var address = config.AllowLan ? "0.0.0.0" : "localhost";
                    Log($"服务器已启动，地址： http://{address}:{widgetServer.ServerPort}");
                }
                catch (Exception ex)
                {
                    Log($"服务器启动失败：${ex.Message}");
                }
            });
        }

        private void WidgetServer_OnDanmakuWebSocketConnected(EmbedIO.WebSockets.IWebSocketContext socket)
        {
            Task.Run(async () =>
            {
                try
                {
                    await widgetServer.SendInitEvent(socket, RoomId, historyDanmakuList);
                }
                catch (Exception ex)
                {
                    Log($"发送历史弹幕失败：{ex.Message}");
                }
            });
        }
    }
}