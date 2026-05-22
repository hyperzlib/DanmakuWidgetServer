using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DanmakuWidgetServer
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly PluginConfig pluginConfig;
        private readonly Plugin pluginMain;
        private bool hasUnsavedChanges = false;
        private bool isInitializing = false;

        public SettingsWindow(PluginConfig pluginConfig, Plugin pluginMain)
        {
            InitializeComponent();

            this.pluginConfig = pluginConfig;
            this.pluginMain = pluginMain;

            LoadConfigToUi();
            UpdateServerButtonState();
        }

        private void LoadConfigToUi()
        {
            isInitializing = true;
            TextBoxHttpPort.Text = pluginConfig.HttpPort.ToString();
            TextBoxTplFolder.Text = pluginConfig.TemplateFolder ?? string.Empty;
            CheckBoxAllowLan.IsChecked = pluginConfig.AllowLan;
            isInitializing = false;

            RefreshUnsavedState();
        }

        private void RefreshUnsavedState()
        {
            var isPortValid = int.TryParse(TextBoxHttpPort.Text.Trim(), out var port);
            hasUnsavedChanges = !isPortValid
                || port != pluginConfig.HttpPort
                || (TextBoxTplFolder.Text ?? string.Empty) != (pluginConfig.TemplateFolder ?? string.Empty)
                || (CheckBoxAllowLan.IsChecked ?? false) != pluginConfig.AllowLan;
            UpdateApplyButtonState();
        }

        private void UpdateApplyButtonState()
        {
            ButtonApply.IsEnabled = hasUnsavedChanges;
        }

        private void UpdateServerButtonState()
        {
            var isRunning = pluginMain.widgetServer != null;
            ButtonStartStopServer.Content = isRunning ? "停止服务器" : "启动服务器";
        }

        private bool TryGetConfigFromUi(out int port, out string templateFolder, out bool allowLan)
        {
            port = 0;
            templateFolder = (TextBoxTplFolder.Text ?? string.Empty).Trim();
            allowLan = CheckBoxAllowLan.IsChecked ?? false;

            if (!int.TryParse(TextBoxHttpPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("HTTP端口必须是 1-65535 的整数。", "参数错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(templateFolder))
            {
                MessageBox.Show("模板目录不能为空。", "参数错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ButtonSelectTplFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "选择模板文件夹..."
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName) && dialog.FileName != TextBoxTplFolder.Text)
            {
                TextBoxTplFolder.Text = dialog.FileName;
            }
        }

        private void TextBoxHttpPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            RefreshUnsavedState();
        }

        private void TextBoxTplFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            RefreshUnsavedState();
        }

        private void CheckBoxAllowLan_Checked(object sender, RoutedEventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            RefreshUnsavedState();
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetConfigFromUi(out var port, out var templateFolder, out var allowLan))
            {
                return;
            }

            try
            {
                if (!Directory.Exists(templateFolder))
                {
                    Directory.CreateDirectory(templateFolder);
                }

                var wasRunning = pluginMain.widgetServer != null;

                pluginConfig.HttpPort = port;
                pluginConfig.TemplateFolder = templateFolder;
                pluginConfig.AllowLan = allowLan;
                pluginConfig.Save(Path.Combine(Plugin.PluginDataPath, "config.json"));

                if (wasRunning)
                {
                    pluginMain.Stop();
                    pluginMain.Start();
                }

                hasUnsavedChanges = false;
                UpdateApplyButtonState();
                UpdateServerButtonState();
                MessageBox.Show("设置已保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存设置失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonStartStopServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (pluginMain.widgetServer == null)
                {
                    pluginMain.Start();
                }
                else
                {
                    pluginMain.Stop();
                }

                UpdateServerButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作服务器失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonOpenTplFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = (TextBoxTplFolder.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show("模板目录为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开目录失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
