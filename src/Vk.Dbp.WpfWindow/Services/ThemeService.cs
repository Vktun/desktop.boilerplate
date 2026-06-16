using System;
using System.Diagnostics;
using System.Windows;
using Dabp.Services.Settings;
using Dabp.Utils.Exceptions;

namespace Dabp.WpfWindow.Services
{
    public interface IThemeService
    {
        string CurrentTheme { get; }

        void SetTheme(string themeName);

        string[] GetAvailableThemes();

        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    }

    public sealed class ThemeChangedEventArgs : EventArgs
    {
        public string OldTheme { get; set; } = string.Empty;

        public string NewTheme { get; set; } = string.Empty;
    }

    public sealed class ThemeService : IThemeService
    {
        private const string LightTheme = "Light";
        private const string DarkTheme = "Dark";
        private const string ThemeKey = "AppTheme";

        private readonly IAppSettingsService _appSettingsService;
        private string _currentTheme;

        public ThemeService(IAppSettingsService appSettingsService)
        {
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            _currentTheme = GetSavedTheme() ?? LightTheme;

            ApplyThemeResourceDictionary(_currentTheme);
            Debug.WriteLine($"[ThemeService] Initialized theme: {_currentTheme}");
        }

        public string CurrentTheme => _currentTheme;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public void SetTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return;
            }

            if (!string.Equals(themeName, LightTheme, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(themeName, DarkTheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(themeName, _currentTheme, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string oldTheme = _currentTheme;
            _currentTheme = themeName;

            try
            {
                ApplyThemeResourceDictionary(themeName);
                SaveTheme(themeName);

                ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                {
                    OldTheme = oldTheme,
                    NewTheme = themeName
                });
            }
            catch (Exception ex) when (
                ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex) ||
                ex is InvalidOperationException ||
                ex is UriFormatException)
            {
                _currentTheme = oldTheme;
                throw;
            }
        }

        public string[] GetAvailableThemes()
        {
            return new[] { LightTheme, DarkTheme };
        }

        private void ApplyThemeResourceDictionary(string themeName)
        {
            Application? app = Application.Current;
            if (app?.Resources is null)
            {
                return;
            }

            var mergedDictionaries = app.Resources.MergedDictionaries;
            for (int index = mergedDictionaries.Count - 1; index >= 0; index--)
            {
                ResourceDictionary dictionary = mergedDictionaries[index];
                string? source = dictionary.Source?.ToString();
                if (string.IsNullOrWhiteSpace(source))
                {
                    continue;
                }

                if (source.EndsWith("/Themes/LightTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                    source.EndsWith("/Themes/DarkTheme.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    mergedDictionaries.RemoveAt(index);
                }
            }

            string uriPath = $"pack://application:,,,/Themes/{themeName}Theme.xaml";
            mergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(uriPath, UriKind.Absolute)
            });
        }

        private string? GetSavedTheme()
        {
            try
            {
                return _appSettingsService.GetValue<string?>(ThemeKey, null);
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex))
            {
                Debug.WriteLine($"[ThemeService] Failed to load theme: {ex.Message}");
                return null;
            }
        }

        private void SaveTheme(string themeName)
        {
            try
            {
                _appSettingsService.SetValue(ThemeKey, themeName);
            }
            catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedFileOperationException(ex))
            {
                Debug.WriteLine($"[ThemeService] Failed to save theme: {ex.Message}");
            }
        }
    }
}
