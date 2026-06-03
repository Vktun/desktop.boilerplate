---
name: "theme-switch"
description: "Handles theme switching between light and dark modes in WPF applications using HandyControl. Invoke when user needs theme toggle functionality, theme persistence, or asks about theme switching."
---

# Theme Switch Skill

## Overview

This skill covers theme switching in Desktop Boilerplate using `IThemeService` / `ThemeService`, supporting light and dark themes with HandyControl resource dictionary swapping and preference persistence.

## Core Components

### IThemeService / ThemeService

Both interface and implementation are defined in `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs`.

```csharp
public interface IThemeService
{
    string CurrentTheme { get; }
    void SetTheme(string themeName);
    void ToggleTheme();
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;
}

public class ThemeService : IThemeService
{
    public const string LightTheme = "Light";
    public const string DarkTheme = "Dark";

    // SetTheme replaces MergedDictionaries entries
    // Persists preference via IAppSettingsService
    // Fires ThemeChanged event
    // Error recovery: rolls back to previous theme on failure
}
```

### Theme Toggle in HeaderViewModel

```csharp
private void handleToggleTheme(string theme)
{
    if (string.IsNullOrEmpty(theme))
        return;

    // If passed theme is current, switch to the other
    if (theme == _themeService.CurrentTheme)
    {
        theme = _themeService.CurrentTheme == IThemeService.LightTheme
            ? IThemeService.DarkTheme
            : IThemeService.LightTheme;
    }

    _themeService.SetTheme(theme);
}
```

## UI Implementation

### Button with Interaction.Triggers

```xaml
<Button Content="Toggle Theme">
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="Click">
            <i:InvokeCommandAction Command="{Binding ToggleThemeCommand}"
                                  CommandParameter="Light"/>
        </i:EventTrigger>
    </i:Interaction.Triggers>
</Button>
```

### Context Menu Option

```xaml
<ToggleButton.ContextMenu>
    <ContextMenu>
        <MenuItem Header="Light Theme"
                  Command="{Binding ToggleThemeCommand}"
                  CommandParameter="Light"/>
        <MenuItem Header="Dark Theme"
                  Command="{Binding ToggleThemeCommand}"
                  CommandParameter="Dark"/>
    </ContextMenu>
</ToggleButton.ContextMenu>
```

## HandyControl Integration

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"/>
            <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

`ThemeService.SetTheme` swaps the `SkinDefault.xaml` resource dictionary between light and dark variants in `Application.Current.Resources.MergedDictionaries`.

## Theme Persistence

- Theme preference is saved via `IAppSettingsService` when `SetTheme` is called.
- On application startup, `ThemeService` constructor reads the saved preference and applies it immediately.

## Theme Colors

### Light Theme
- Background: #f0f4f8 ~ #e2e8f0
- Primary Text: #2d3748
- Secondary Text: #4a5568
- Border: #e2e8f0

### Dark Theme
- Background: #1a202c ~ #2d3748
- Primary Text: #f7fafc
- Secondary Text: #e2e8f0
- Border: #4a5568

## Key Files

| Component | Location |
|-----------|----------|
| IThemeService + ThemeService | `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs` |
| ThemeChangedEventArgs | `src/Vk.Dbp.WpfWindow/Services/ThemeService.cs` |
| HeaderView | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| HeaderViewModel | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |
| AppSettingsService | `src/Vk.Dbp.Services/Settings/` |

## Common Issues and Solutions

### Issue: Button click not triggering theme change
**Solution**: Use `Interaction.Triggers` instead of direct Command binding for ToggleButton.

### Issue: Theme not persisting across restarts
**Solution**: Ensure `IAppSettingsService` is registered and the ThemeService constructor reads the saved preference.

### Issue: Some controls not updating after theme switch
**Solution**: Ensure controls use `DynamicResource` for theme-sensitive properties, not `StaticResource`.

### Issue: Theme switch throws and leaves app in broken state
**Solution**: ThemeService has built-in rollback — if applying the new theme fails, it restores the previous MergedDictionaries. Do not remove this safety behavior.

## Testing

```csharp
[Fact]
public void SetTheme_Light_SetsCurrentTheme()
{
    var mockSettings = new Mock<IAppSettingsService>();
    var themeService = new ThemeService(mockSettings.Object);
    themeService.SetTheme("Light");
    themeService.CurrentTheme.Should().Be("Light");
}

[Fact]
public void ToggleTheme_SwitchesBetweenLightAndDark()
{
    var mockSettings = new Mock<IAppSettingsService>();
    var themeService = new ThemeService(mockSettings.Object);
    themeService.SetTheme("Light");
    themeService.ToggleTheme();
    themeService.CurrentTheme.Should().Be("Dark");
}
```
