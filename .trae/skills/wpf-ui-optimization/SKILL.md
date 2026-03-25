---
name: "wpf-ui-optimization"
description: "Optimizes WPF UI performance including layout, binding, rendering, and resource optimization. Invoke when user asks for WPF UI optimization or performance improvement."
---

# WPF UI Optimization Skill

## Overview

This skill provides guidance for optimizing WPF application UI performance, covering layout, data binding, rendering, and resource management.

## Optimization Areas

### 1. Layout Optimization

- **Simplify Grid and DockPanel usage**: Reduce unnecessary nesting
- **Optimize RowDefinition/ColumnDefinition**: Prefer fixed values or ratios over Auto
- **Remove unnecessary containers**: Simplify layout hierarchy

### 2. Data Virtualization

- **Enable virtualization for list controls**:
  ```xaml
  VirtualizingStackPanel.IsVirtualizing="True"
  VirtualizingStackPanel.VirtualizationMode="Recycling"
  ```
- **Implement data pagination**: Reduce one-time data loading
- **Async data loading**: Use async methods in ViewModel

### 3. Binding Optimization

- **Use OneWay binding**: For display-only data
- **Efficient INotifyPropertyChanged**: Use PropertyChanged.Fody
- **Avoid unnecessary bindings**: Only bind required data

### 4. Rendering Optimization

- **Enable GPU acceleration**: Modern WPF uses hardware acceleration by default
- **Simplify visual effects**: Reduce complex gradients and shadows
- **Use caching**: `CacheMode="BitmapCache"` for static UI elements

### 5. Resource Optimization

- **Optimize images**: Use appropriate size and format
- **Merge resource dictionaries**: Centralize style management
- **Remove unused resources**: Clean up project resources

### 6. View Optimization

- **Lazy loading**: Load complex views on demand
- **View caching**: Cache frequently switched views
- **Optimize ViewModel creation**: Avoid creating too many objects on load

## Color Scheme Optimization

### Comfortable Light Theme Colors

| Element | Color Code | Usage |
|---------|------------|-------|
| Background | #f0f4f8 ~ #e2e8f0 | Main window gradient |
| Header Background | #ffffff | Header panel |
| Primary Text | #2d3748 | Main headings |
| Secondary Text | #4a5568 | Subheadings, descriptions |
| Border | #e2e8f0 | Dividers, borders |

### XAML Example

```xaml
<Grid>
    <Grid.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
            <GradientStop Color="#f0f4f8" Offset="0"/>
            <GradientStop Color="#e2e8f0" Offset="1"/>
        </LinearGradientBrush>
    </Grid.Background>
</Grid>
```

## Expected Results

- Improved UI response speed
- Reduced memory usage
- Faster application startup
- Smoother user experience

## Testing Plan

- Performance testing with Visual Studio Profiler
- Functional testing after optimization
- User experience evaluation
