# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Vk.Dbp.Contracts项目** - 模块契约定义层
  - `IModuleMetadata` - 模块元数据接口
  - `IViewModelFactory` - ViewModel工厂接口
  - `IExportService` - 数据导出服务接口
  - `ICacheService` - 缓存服务接口
  - `IPagedQuery` - 分页查询接口
- **扩展点系统**
  - `IMenuItemProvider` - 自定义菜单扩展
  - `IDashboardWidgetProvider` - 仪表盘组件扩展
  - `IReportGenerator` - 报表生成扩展
- **测试框架** - xUnit单元测试和集成测试
- **GitHub Actions CI** - 自动化构建和测试
- **代码覆盖率** - Coverlet集成，目标覆盖率≥40%
- **模块开发指南** - 详细的MODULE_DEVELOPMENT_GUIDE.md

### Changed
- **UserSession重构** - 从单例模式改为纯依赖注入
- **异步初始化** - 移除同步阻塞异步代码，改为Task.Run异步模式
- **Token安全** - 使用加密安全的RandomNumberGenerator替代GUID

### Fixed
- **编码问题** - LoginViewModel等文件中文乱码
- **内存泄漏** - HeaderViewModel事件订阅未取消
- **安全问题**
  - 移除硬编码弱密码（"123456", "admin123"）
  - 禁用危险的系统关机命令

### Security
- 密码重置功能改为生成随机强密码
- 默认管理员密码改为随机生成并记录到日志
- Token生成使用加密安全随机数

## [0.1.0] - 2026-04-01

### Added
- 初始版本发布
- 基于Prism的模块化架构
- RBAC权限管理系统（用户-角色-权限）
- 审计日志功能
- 主题切换（Light/Dark）
- 用户管理、角色管理、权限管理
- 登录会话管理
- 组织架构管理

### Known Issues
- 测试覆盖率较低
- 部分文档待完善
- 存在N+1查询性能问题

---

## Version History

| Version | Release Date | Key Changes |
|---------|--------------|-------------|
| 0.1.0 | 2026-04-01 | Initial release |
| 0.2.0 | 2026-04-06 | Modular architecture and security improvements |

---

## Upgrade Guide

### Upgrading from 0.1.0 to 0.2.0

#### Breaking Changes

1. **UserSession单例模式移除**

   **Before:**
   ```csharp
   var session = UserSession.Instance;
   ```

   **After:**
   ```csharp
   public class YourViewModel
   {
       private readonly IUserSession _userSession;
       
       public YourViewModel(IUserSession userSession)
       {
           _userSession = userSession;
       }
   }
   ```

2. **新增Contracts项目**

   所有模块应引用 `Vk.Dbp.Contracts` 项目以使用模块接口。

#### New Features

1. **使用ViewModel工厂**
   ```csharp
   // 不推荐
   var vm = new YourDialogViewModel();
   
   // 推荐
   var vm = _viewModelFactory.Create<YourDialogViewModel>();
   ```

2. **使用缓存服务**
   ```csharp
   var data = await _cacheService.GetOrCreateAsync(
       "cache_key",
       () => _db.Queryable<Data>().ToListAsync(),
       TimeSpan.FromMinutes(5)
   );
   ```

3. **实现模块扩展点**
   ```csharp
   public class YourMenuProvider : IMenuItemProvider
   {
       public IEnumerable<MenuItemInfo> GetMenuItems()
       {
           yield return new MenuItemInfo { ... };
       }
   }
   ```

---

[Unreleased]: https://github.com/yourorg/desktop.boilerplate/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/yourorg/desktop.boilerplate/releases/tag/v0.1.0