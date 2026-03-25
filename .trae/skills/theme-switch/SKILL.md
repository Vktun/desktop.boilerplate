---
name: "theme-switch"
description: "Handles theme switching between light and dark modes in WPF applications. Invoke when user needs theme toggle functionality or asks about theme switching."
---

# Theme Switch Skill

## Overview

This skill provides theme switching functionality for WPF applications using HandyControl, supporting both light and dark themes with smooth transitions.

## Core Components

### ThemeService

```csharp
public class ThemeService : IThemeService
{
    public const string LightTheme = "Light";
    public const string DarkTheme = "Dark";
    
    public string CurrentTheme { get; private set; }
    
    public void SetTheme(string themeName);
    public void ToggleTheme();
}
```

### Theme Toggle Implementation

```csharp
private void handleToggleTheme(string theme)
{
    if (string.IsNullOrEmpty(theme))
        return;

    // If passed theme is current, switch to the other
    if (theme == _themeService.CurrentTheme)
    {
        theme = _themeService.CurrentTheme == LightTheme ? DarkTheme : LightTheme;
    }

    _themeService.SetTheme(theme);
}
```

## UI Implementation

### Button with Click Event

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
| ThemeService | `Services/ThemeService.cs` |
| IThemeService | `Services/IThemeService.cs` |
| HeaderView | `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` |
| HeaderViewModel | `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` |

## Common Issues and Solutions

### Issue: Button click not triggering theme change
**Solution**: Use `Interaction.Triggers` instead of direct Command binding

### Issue: Theme not persisting
**Solution**: Save theme preference to configuration file

### Issue: Some controls not updating
**Solution**: Ensure controls use DynamicResource for theme-sensitive properties

## Testing

```csharp
// Test theme toggle
var themeService = new ThemeService();
themeService.SetTheme("Light");
Assert.AreEqual("Light", themeService.CurrentTheme);

themeService.ToggleTheme();
Assert.AreEqual("Dark", themeService.CurrentTheme);
```
