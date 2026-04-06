using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Dabp.Services.Settings;

public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _lockObject = new();
    private readonly string _settingsFilePath;
    private Dictionary<string, JsonElement> _settings;

    public AppSettingsService()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "DabpDesktopBoilerplate";
        string settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);

        Directory.CreateDirectory(settingsDirectory);
        _settingsFilePath = Path.Combine(settingsDirectory, "settings.json");
        _settings = LoadSettings();
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        lock (_lockObject)
        {
            if (!_settings.TryGetValue(key, out JsonElement value))
            {
                return defaultValue;
            }

            try
            {
                T? result = value.Deserialize<T>(JsonOptions);
                return result is null ? defaultValue : result;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    public void SetValue<T>(string key, T value)
    {
        lock (_lockObject)
        {
            _settings[key] = JsonSerializer.SerializeToElement(value, JsonOptions);
            SaveSettings();
        }
    }

    public bool Remove(string key)
    {
        lock (_lockObject)
        {
            bool removed = _settings.Remove(key);
            if (removed)
            {
                SaveSettings();
            }

            return removed;
        }
    }

    private Dictionary<string, JsonElement> LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            Dictionary<string, JsonElement>? loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
            return loaded ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveSettings()
    {
        string json = JsonSerializer.Serialize(_settings, JsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }
}
