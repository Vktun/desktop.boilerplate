# Desktop Boilerplate - Dabp

一个现代化的 WPF 企业级桌面应用框架

## 🚀 快速开始

### 技术栈
- **Framework**: .NET 10 / .NET Standard 2.1 / .NET Standard 2.0
- **UI Framework**: WPF + Prism + HandyControl
- **Architecture**: MVVM Pattern
- **DI**: Prism IoC Container

### 项目结构

## 📖 文档

- **[skill.md](./docs/skill.md)** - 完整的开发指南、设计模式、常用Skill列表
  - 详细的架构说明
  - 8个核心设计模式
  - 8个通用开发Skill
  - 常见问题解答

## 🔧 开发规范

### ViewModel 编写

### View 绑定

## 📝 常见任务

### 添加新视图
1. 创建 `Views/MyView.xaml`
2. 创建 `ViewModels/MyViewModel.cs`
3. 在 `App.xaml.cs` 中注册：`containerRegistry.RegisterForNavigation<MyView, MyViewModel>("MyView")`
4. 在需要的地方导航：`_regionManager.RequestNavigate("ContentRegion", "MyView")`

### 添加服务
1. 创建 `Services/Interfaces/IMyService.cs`
2. 实现 `Services/MyService.cs`
3. 在 `App.xaml.cs` 中注册：`containerRegistry.Register<IMyService, MyService>()`
4. 在ViewModel中注入使用

### 修改UI样式
参见 `docs/skill.md` 中的 **Skill 5: UI样式定义**

## 🤝 贡献指南

1. 遵循项目的MVVM架构
2. 参考 `docs/skill.md` 中的最佳实践
3. 使用有意义的命名约定
4. 添加必要的代码注释

## 📞 支持

遇到问题？查看 `docs/skill.md` 中的 **常见问题和最佳实践** 部分

## 📄 许可证

MIT License