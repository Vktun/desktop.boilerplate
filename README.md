# Desktop Boilerplate (Dabp)

[![CI Status](https://github.com/yourorg/desktop.boilerplate/workflows/CI/badge.svg)](https://github.com/yourorg/desktop.boilerplate/actions)
[![Code Coverage](https://codecov.io/gh/yourorg/desktop.boilerplate/branch/main/graph/badge.svg)](https://codecov.io/gh/yourorg/desktop.boilerplate)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**企业级WPF桌面应用框架** - 基于Prism + HandyControl，支持模块化插件架构，开箱即用。

## ✨ 特性

- 🏗️ **模块化架构** - 基于Prism的动态模块加载，支持插件化开发
- 🔐 **完善的RBAC** - 用户-角色-权限三级权限管理
- 🎨 **现代化UI** - HandyControl组件库，支持Light/Dark主题切换
- 🔌 **丰富的扩展点** - 菜单、仪表盘、报表等均可扩展，无需修改核心代码
- 📊 **审计追踪** - 完整的操作审计日志
- 🧪 **测试完备** - xUnit单元测试 + 集成测试，CI/CD自动化
- 🚀 **性能优化** - 智能缓存、分页查询、异步非阻塞

## 🚀 快速开始

### 前置要求

- .NET 10 SDK
- Visual Studio 2022+ 或 JetBrains Rider
- SQL Server 2019+（或使用SQLite进行开发测试）

### 克隆和构建

```bash
# 克隆仓库
git clone https://github.com/yourorg/desktop.boilerplate.git
cd desktop.boilerplate

# 还原依赖
dotnet restore

# 构建项目
dotnet build
```

### 配置数据库

1. 复制配置模板
   ```bash
   cd src/Vk.Dbp.WpfWindow
   cp appsettings.local.example.json appsettings.local.json
   ```

2. 编辑 `appsettings.local.json`，修改连接字符串
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=127.0.0.1;Database=DabpDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. 运行应用，数据库将自动初始化

### 默认账户

首次启动时，系统会生成随机密码并记录到日志中：

```bash
# 查看日志获取初始密码
# Windows: %LOCALAPPDATA%\Vk.Dbp.WpfWindow\Logs\logs-YYYYMMDD.txt
# 日志内容示例：
# [Warning] 默认管理员账户已创建 - 用户名: admin, 初始密码: xY9#mK2!pL5@ (请立即修改)
```

## 📖 文档

| 文档 | 说明 |
|------|------|
| [快速入门](docs/QUICKSTART.md) | 5分钟上手指南 |
| [架构设计](docs/ARCHITECTURE.md) | 系统架构详解 |
| [模块开发指南](docs/MODULE_DEVELOPMENT_GUIDE.md) | **⭐ 如何开发自定义模块** |
| [API参考](docs/API_REFERENCE.md) | 核心API文档 |
| [常见问题](docs/FAQ.md) | FAQ |

## 🏗️ 项目结构

```
desktop.boilerplate/
├── src/                          # 核心源代码
│   ├── Vk.Dbp.WpfWindow/        # 主应用程序（Shell）
│   ├── Vk.Dbp.Contracts/        # 接口和契约定义 ⭐
│   ├── Vk.Dbp.Services/         # 通用服务实现
│   ├── Vk.Dbp.Infrastructure/   # 基础设施（数据库、日志）
│   ├── Vk.Dbp.Domain/           # 领域模型
│   └── Vk.Dbp.Utils/            # 工具类
│
├── prismModules/                 # Prism模块
│   ├── Vk.Dbp.AccountModule/    # 账户管理模块
│   └── Vk.Dbp.WorkshopModule/   # 车间管理模块
│
├── dbpframework/                 # 框架层
│   ├── Vk.Dbp.Core/             # 核心抽象
│   └── Vk.Dbp.Account/          # 账户领域核心
│
├── dbpApps/                      # 示例应用
│   ├── Dbp.Material.Forming/    # 成型工艺应用
│   └── Dbp.Material.Mixing/     # 混合工艺应用
│
├── test/                         # 测试项目
│   ├── Vk.Dbp.Tests.Unit/       # 单元测试
│   ├── Vk.Dbp.Tests.Integration/# 集成测试
│   └── Vk.Dbp.Tests.Common/     # 测试辅助类
│
└── docs/                         # 文档
```

## 🛠️ 开发指南

### 添加新功能模块

参见 [模块开发指南](docs/MODULE_DEVELOPMENT_GUIDE.md) - **二开友好性核心文档**

快速示例：

```csharp
// 1. 实现IModuleMetadata
public class YourModule : IModule, IModuleMetadata
{
    public string ModuleName => "YourModule";
    public string[] Dependencies => new[] { "AccountModule" };
    
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IYourService, YourService>();
        containerRegistry.RegisterForNavigation<YourView>();
    }
}

// 2. 实现扩展点
public class YourMenuProvider : IMenuItemProvider
{
    public IEnumerable<MenuItemInfo> GetMenuItems()
    {
        yield return new MenuItemInfo
        {
            Name = "your_feature",
            DisplayName = "自定义功能",
            NavigateTo = "YourView"
        };
    }
}
```

### 代码规范

- **命名约定**: 遵循C#命名规范（PascalCase for classes, camelCase for locals）
- **文档注释**: 所有公共API必须有XML文档注释
- **依赖注入**: 使用DI，避免直接 `new`
- **ViewModel**: 不包含业务逻辑，委托给Service层

### 提交规范

使用 Conventional Commits:

```
feat: 添加用户导出功能
fix: 修复登录页面编码问题
docs: 更新模块开发指南
refactor: 重构UserSession为纯DI模式
test: 添加UserService单元测试
```

## 🤝 贡献

欢迎贡献！请先阅读 [贡献指南](CONTRIBUTING.md)

## 📄 许可证

MIT License - 详见 [LICENSE](LICENSE)

## 🌟 致谢

本项目基于以下优秀的开源项目构建：

- [Prism](https://github.com/PrismLibrary/Prism) - MVVM框架
- [HandyControl](https://github.com/HandyOrg/HandyControl) - WPF控件库
- [SqlSugar](https://github.com/DotNetNext/SqlSugar) - ORM框架
- [Serilog](https://github.com/serilog/serilog) - 日志框架

---

**Made with ❤️ by Your Team**