using System;
using System.Windows;
using System.Diagnostics;

namespace Dabp.WpfWindow.Services
{
    /// <summary>
    /// 主题服务 - 管理应用主题切换
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 当前主题
        /// </summary>
        string CurrentTheme { get; }

        /// <summary>
        /// 切换主题
        /// </summary>
        void SetTheme(string themeName);

        /// <summary>
        /// 获取主题列表
        /// </summary>
        string[] GetAvailableThemes();

        /// <summary>
        /// 主题变更事件
        /// </summary>
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    }

    /// <summary>
    /// 主题变更事件参数
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string OldTheme { get; set; }
        public string NewTheme { get; set; }
    }

    /// <summary>
    /// 主题服务实现
    /// </summary>
    public class ThemeService : IThemeService
    {
        private const string LightTheme = "Light";
        private const string DarkTheme = "Dark";
        private const string ThemeKey = "AppTheme";

        private string _currentTheme;

        public string CurrentTheme => _currentTheme;

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        public ThemeService()
        {
            // 从配置中读取或使用默认主题
            _currentTheme = GetSavedTheme() ?? LightTheme;
            Debug.WriteLine($"[ThemeService] 初始化主题: {_currentTheme}");
        }

        public void SetTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                Debug.WriteLine("[ThemeService] SetTheme: 主题名称为空");
                return;
            }

            if (themeName == _currentTheme)
            {
                Debug.WriteLine($"[ThemeService] SetTheme: 主题已是 {themeName}，无需切换");
                return;
            }

            // 验证主题名称是否有效
            if (themeName != LightTheme && themeName != DarkTheme)
            {
                Debug.WriteLine($"[ThemeService] SetTheme: 无效的主题名称: {themeName}");
                return;
            }

            string oldTheme = _currentTheme;
            _currentTheme = themeName;

            try
            {
                Debug.WriteLine($"[ThemeService] 正在切换主题: {oldTheme} -> {themeName}");

                // 加载对应的资源字典
                LoadThemeResourceDictionary(themeName);

                // 保存主题选择
                SaveTheme(themeName);

                Debug.WriteLine($"[ThemeService] 主题切换成功: {themeName}");

                // 触发事件
                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                {
                    OldTheme = oldTheme,
                    NewTheme = themeName
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] 主题切换失败: {ex.Message}");
                _currentTheme = oldTheme;
                throw;
            }
        }

        public string[] GetAvailableThemes()
        {
            return new[] { LightTheme, DarkTheme };
        }

        /// <summary>
        /// 加载主题资源字典
        /// </summary>
        private void LoadThemeResourceDictionary(string themeName)
        {
            var app = Application.Current;
            if (app == null)
            {
                Debug.WriteLine("[ThemeService] LoadThemeResourceDictionary: Application.Current 为空");
                return;
            }

            var rd = app.Resources;
            if (rd == null)
            {
                Debug.WriteLine("[ThemeService] LoadThemeResourceDictionary: Resources 为空");
                return;
            }

            // 移除现有主题字典 (保留前两个默认字典: HandyControl和LightTheme)
            if (rd.MergedDictionaries.Count > 2)
            {
                Debug.WriteLine($"[ThemeService] 移除旧主题字典，当前字典数: {rd.MergedDictionaries.Count}");
                rd.MergedDictionaries.RemoveAt(2);
            }

            // 根据主题名加载对应资源字典
            string uriPath = $"pack://application:,,,/Themes/{themeName}Theme.xaml";
            try
            {
                Debug.WriteLine($"[ThemeService] 加载主题资源: {uriPath}");
                var newThemeDict = new ResourceDictionary { Source = new Uri(uriPath) };
                rd.MergedDictionaries.Add(newThemeDict);
                Debug.WriteLine($"[ThemeService] 主题资源加载成功");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] 主题资源加载失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取保存的主题
        /// </summary>
        private string GetSavedTheme()
        {
            try
            {
                // 从应用设置中读取 (可根据实际项目修改)
                object theme = Application.Current.Properties[ThemeKey];
                string result = theme?.ToString();
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.WriteLine($"[ThemeService] GetSavedTheme: {result}");
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] GetSavedTheme 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存主题选择
        /// </summary>
        private void SaveTheme(string themeName)
        {
            try
            {
                Application.Current.Properties[ThemeKey] = themeName;
                Debug.WriteLine($"[ThemeService] 主题保存成功: {themeName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] 主题保存失败: {ex.Message}");
                // 忽略保存失败
            }
        }
    }
}
