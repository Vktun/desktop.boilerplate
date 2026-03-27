# 下一步功能完善规划 Spec

## Why
当前项目已完成基础架构搭建，包括 Prism 模块化框架、登录功能、用户会话管理、审计日志服务等。但多个核心功能仍处于骨架状态，需要完善数据持久化、视图注册、权限系统等关键功能，使项目具备完整的企业级应用能力。

## What Changes
- 完善数据持久化层，实现 SqlSugar 数据库集成
- 完善账户模块视图注册和导航功能
- 实现基于角色的权限控制系统
- 完善后台管理界面功能
- 增强密码安全机制
- 实现通知系统基础功能
- 完善组织架构管理功能

## Impact
- Affected specs: 数据访问层、账户模块、权限系统、后台管理
- Affected code: 
  - `src/Vk.Dbp.Infrastructure/` - 数据访问层
  - `prismModules/Vk.Dbp.AccountModule/` - 账户模块
  - `src/Vk.Dbp.WpfWindow/` - 主窗口和布局
  - `prismModules/Vk.Dbp.WorkshopModule/` - 车间模块

## ADDED Requirements

### Requirement: 数据持久化层完善
系统 SHALL 提供 SqlSugar 数据库访问实现，替代当前的内存数据存储。

#### Scenario: 用户数据持久化
- **WHEN** 创建、更新或删除用户
- **THEN** 数据应持久化到 SqlServer 数据库

#### Scenario: 角色和权限数据持久化
- **WHEN** 管理角色和权限
- **THEN** 数据应通过 SqlSugar ORM 进行持久化

### Requirement: 账户模块视图导航
系统 SHALL 提供完整的账户管理视图导航功能。

#### Scenario: 导航到用户管理
- **WHEN** 用户点击后台管理中的用户管理菜单
- **THEN** 系统应导航到 UserManagementView

#### Scenario: 导航到角色管理
- **WHEN** 用户点击后台管理中的角色管理菜单
- **THEN** 系统应导航到 RoleManagementView

#### Scenario: 导航到权限管理
- **WHEN** 用户点击后台管理中的权限管理菜单
- **THEN** 系统应导航到 PermissionManagementView

### Requirement: 权限控制系统
系统 SHALL 提供基于角色的权限控制功能。

#### Scenario: 权限验证
- **WHEN** 用户访问需要权限的功能
- **THEN** 系统应验证用户是否具有相应权限

#### Scenario: 菜单权限过滤
- **WHEN** 用户登录系统
- **THEN** 系统应根据用户角色显示可访问的菜单

### Requirement: 密码安全机制
系统 SHALL 提供安全的密码存储和验证机制。

#### Scenario: 密码哈希存储
- **WHEN** 用户设置或修改密码
- **THEN** 系统应使用安全的哈希算法存储密码

#### Scenario: 密码验证
- **WHEN** 用户登录时输入密码
- **THEN** 系统应使用哈希验证而非明文比较

### Requirement: 通知系统
系统 SHALL 提供应用内通知功能。

#### Scenario: 显示通知
- **WHEN** 系统有重要消息需要通知用户
- **THEN** 应在通知区域显示通知内容

#### Scenario: 通知计数
- **WHEN** 有未读通知
- **THEN** 通知图标应显示未读数量

### Requirement: 组织架构管理
系统 SHALL 提供组织架构管理功能。

#### Scenario: 创建组织单元
- **WHEN** 管理员创建新的组织单元
- **THEN** 系统应保存组织单元信息

#### Scenario: 用户归属组织
- **WHEN** 分配用户到组织单元
- **THEN** 用户应关联到指定组织

## MODIFIED Requirements

### Requirement: UserService 数据访问
UserService 应从内存存储改为 SqlSugar 数据库访问。

**原实现**: 使用 `List<User>` 内存存储
**新实现**: 使用 `ISqlSugarClient` 进行数据库操作

### Requirement: AccountModule 视图注册
AccountModule 应注册所有管理视图的导航。

**原实现**: 视图注册代码被注释
**新实现**: 取消注释并完善视图注册

## REMOVED Requirements

### Requirement: 内存示例数据
**Reason**: 数据持久化后将使用真实数据库数据
**Migration**: 保留初始化脚本用于开发环境数据初始化
