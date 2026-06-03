---
name: "wpf-ui-optimization"
description: "Optimizes WPF UI performance including layout, binding, rendering, and resource optimization. Invoke when user asks for WPF UI optimization or performance improvement."
---

# WPF UI Optimization Skill

## Overview

This skill provides guidance for optimizing WPF application UI performance in the Desktop Boilerplate project, covering layout, data binding, rendering, resource management, and ViewModel patterns.

## Optimization Areas

### 1. Layout Optimization

- **Simplify Grid and DockPanel usage**: Reduce unnecessary nesting.
- **Optimize RowDefinition/ColumnDefinition**: Prefer fixed values or ratios over `Auto` where possible.
- **Remove unnecessary containers**: Simplify layout hierarchy — avoid wrapping a single child in a StackPanel inside a Grid.
- **Use VirtualizingStackPanel for lists**: Enable virtualization for all `ListBox`, `ListView`, `DataGrid` controls.

### 2. Data Virtualization

- **Enable virtualization for list controls**:
  ```xaml
  <ListBox VirtualizingStackPanel.IsVirtualizing="True"
           VirtualizingStackPanel.VirtualizationMode="Recycling" />
  ```
- **Implement data pagination**: Reduce one-time data loading via service-layer paging.
- **Async data loading**: Use async methods in ViewModel, show loading state while awaiting.

### 3. Binding Optimization

- **Use OneWay binding** for display-only data; TwoWay only when user input is needed.
- **Efficient INotifyPropertyChanged**: Use `SetProperty(ref _field, value)` from `BindableBase`; PropertyChanged.Fody handles auto-properties.
- **Avoid unnecessary bindings**: Only bind required data; remove unused bindings.
- **Use `ObservesProperty`** on `DelegateCommand` instead of manually calling `RaiseCanExecuteChanged`.

### 4. Rendering Optimization

- **Enable GPU acceleration**: Modern WPF uses hardware acceleration by default; ensure `RenderOptions` are not disabled.
- **Simplify visual effects**: Reduce complex gradients, shadows, and opacity masks.
- **Use caching**: `CacheMode="BitmapCache"` for static UI elements that are frequently re-rendered.
- **Freeze freezable resources**: Use `Frozen="True"` on brushes and pens in XAML when they won't change.

### 5. Resource Optimization

- **Optimize images**: Use appropriate size and format; consider `BitmapImage` with `DecodePixelWidth` for large images.
- **Merge resource dictionaries**: Centralize style management; avoid duplicate resource entries.
- **Remove unused resources**: Clean up project resources and styles that are no longer referenced.

### 6. View and ViewModel Optimization

- **Lazy loading**: Load complex views on demand using `RegisterForNavigation` and region navigation.
- **View caching**: Prism can cache navigated views in regions; use `KeepAlive=true` for frequently switched views.
- **Optimize ViewModel creation**: Avoid creating heavy objects in constructors; defer to `OnNavigatedTo` or lazy initialization.
- **Dispose ViewModels properly**: Implement `IDisposable` with `_isDisposed` guard; unsubscribe from events and `IEventAggregator` subscriptions.

### 7. Thread Safety

- **UI updates must be on UI thread**: Wrap event callback UI updates in `Application.Current.Dispatcher.Invoke()`.
- **Avoid blocking UI thread**: Use `async/await` for I/O operations; never call `.Result` or `.Wait()` on async methods from the UI thread.
- **Background processing**: Use `Task.Run` for CPU-bound work, then dispatch results back to UI thread.

## Color Scheme Reference

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

## Performance Checklist

- [ ] List controls use `VirtualizingStackPanel` with `Recycling` mode
- [ ] Display-only bindings use `Mode=OneWay`
- [ ] No synchronous I/O on UI thread
- [ ] Event subscriptions are properly disposed
- [ ] Large images use `DecodePixelWidth`/`DecodePixelHeight`
- [ ] Freezable resources are frozen when possible
- [ ] No unnecessary layout nesting
- [ ] Heavy ViewModel initialization deferred from constructor

## Testing Plan

- Performance testing with Visual Studio Profiler or dotTrace
- Functional testing after optimization
- Memory profiling for long-running sessions
- User experience evaluation for perceived responsiveness
