namespace Dabp.Services.Settings;

public interface IAppSettingsService
{
    T GetValue<T>(string key, T defaultValue);

    void SetValue<T>(string key, T value);

    bool Remove(string key);
}
