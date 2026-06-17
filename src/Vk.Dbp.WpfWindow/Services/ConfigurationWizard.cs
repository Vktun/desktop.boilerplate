using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 配置向导 - 首次启动时引导用户配置
    /// </summary>
    public class ConfigurationWizard
    {
        private const string LocalConfigFileName = "appsettings.local.json";
        private readonly string _configFilePath;
        
        public ConfigurationWizard()
        {
            var appDataPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppDomain.CurrentDomain.FriendlyName
            );
            Directory.CreateDirectory(appDataPath);
            _configFilePath = Path.Join(appDataPath, "appsettings.wizard.json");
        }
        
        /// <summary>
        /// 检查是否需要显示配置向导
        /// </summary>
        public bool ShouldShowWizard()
        {
            // 检查是否存在appsettings.local.json
            var localConfigPath = GetLocalConfigPath();
            if (File.Exists(localConfigPath))
            {
                return false;
            }
            
            // 检查是否完成了向导
            return !File.Exists(_configFilePath);
        }
        
        /// <summary>
        /// 显示配置向导
        /// </summary>
        public bool ShowWizard()
        {
            var dialog = new Window
            {
                Title = "首次运行配置向导",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            
            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            
            // 标题
            var titleBlock = new System.Windows.Controls.TextBlock
            {
                Text = "欢迎使用 Desktop Boilerplate",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            grid.Children.Add(titleBlock);
            
            // 说明
            var descBlock = new System.Windows.Controls.TextBlock
            {
                Text = "检测到这是首次运行，请配置数据库连接信息：",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 40, 0, 10)
            };
            grid.Children.Add(descBlock);
            
            // 数据库服务器
            var serverLabel = new System.Windows.Controls.TextBlock
            {
                Text = "数据库服务器:",
                Margin = new Thickness(0, 80, 0, 5)
            };
            grid.Children.Add(serverLabel);
            
            var serverTextBox = new System.Windows.Controls.TextBox
            {
                Text = "127.0.0.1",
                Margin = new Thickness(0, 105, 0, 10)
            };
            grid.Children.Add(serverTextBox);
            
            // 数据库名称
            var dbNameLabel = new System.Windows.Controls.TextBlock
            {
                Text = "数据库名称:",
                Margin = new Thickness(0, 135, 0, 5)
            };
            grid.Children.Add(dbNameLabel);
            
            var dbNameTextBox = new System.Windows.Controls.TextBox
            {
                Text = "DabpDb",
                Margin = new Thickness(0, 160, 0, 10)
            };
            grid.Children.Add(dbNameTextBox);
            
            // 按钮
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 300, 0, 0)
            };
            
            var okButton = new System.Windows.Controls.Button
            {
                Content = "确定",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            okButton.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            cancelButton.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(buttonPanel);
            
            dialog.Content = grid;
            
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                // 生成配置文件
                var server = serverTextBox.Text;
                var dbName = dbNameTextBox.Text;
                
                var configContent = $@"{{
  ""ConnectionStrings"": {{
    ""Default"": ""Server={server};Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;""
  }}
}}";
                
                var localConfigPath = GetLocalConfigPath();
                File.WriteAllText(localConfigPath, configContent);
                
                // 标记向导已完成
                File.WriteAllText(_configFilePath, "{ \"wizardCompleted\": true }");
                
                return true;
            }
            
            return false;
        }

        private static string GetLocalConfigPath()
        {
            return Path.Join(AppContext.BaseDirectory, LocalConfigFileName);
        }
    }
}
