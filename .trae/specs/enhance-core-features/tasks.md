# Tasks

## 阶段一：数据持久化层完善

- [x] Task 1: 完善基础设施模块 (Vk.Dbp.Infrastructure)
  - [x] SubTask 1.1: 完善 DbpInfrastructureModule 模块注册
  - [x] SubTask 1.2: 创建通用仓储接口 IRepository<T>
  - [x] SubTask 1.3: 实现 SqlSugarRepository<T> 仓储实现
  - [x] SubTask 1.4: 创建数据库初始化服务

- [x] Task 2: 重构 UserService 使用数据库
  - [x] SubTask 2.1: 注入 ISqlSugarClient 替代内存列表
  - [x] SubTask 2.2: 实现基于数据库的 CRUD 操作
  - [x] SubTask 2.3: 实现用户角色关联查询

- [x] Task 3: 实现 RoleService 和 PermissionService 数据库访问
  - [x] SubTask 3.1: 实现 RoleService 数据库操作
  - [x] SubTask 3.2: 实现 PermissionService 数据库操作
  - [x] SubTask 3.3: 实现角色-权限关联管理

## 阶段二：账户模块视图完善

- [x] Task 4: 完善账户模块视图注册
  - [x] SubTask 4.1: 取消 AccountModule 中视图注册的注释
  - [x] SubTask 4.2: 确保所有视图正确绑定 ViewModel
  - [x] SubTask 4.3: 验证导航功能正常工作

- [x] Task 5: 完善 UserManagementView 界面
  - [x] SubTask 5.1: 实现用户列表 DataGrid 显示
  - [x] SubTask 5.2: 实现新增/编辑用户对话框
  - [x] SubTask 5.3: 实现删除确认对话框
  - [x] SubTask 5.4: 实现用户搜索功能

- [x] Task 6: 完善 RoleManagementView 界面
  - [x] SubTask 6.1: 实现角色列表显示
  - [x] SubTask 6.2: 实现角色新增/编辑功能
  - [x] SubTask 6.3: 实现角色权限分配界面

- [x] Task 7: 完善 PermissionManagementView 界面
  - [x] SubTask 7.1: 实现权限列表显示
  - [x] SubTask 7.2: 实现权限新增/编辑功能
  - [x] SubTask 7.3: 实现权限分类管理

## 阶段三：权限控制系统

- [x] Task 8: 实现权限验证服务
  - [x] SubTask 8.1: 创建 IPermissionChecker 接口
  - [x] SubTask 8.2: 实现 PermissionChecker 服务
  - [x] SubTask 8.3: 在 UserSession 中添加权限缓存

- [x] Task 9: 实现菜单权限过滤
  - [x] SubTask 9.1: 创建菜单权限配置
  - [x] SubTask 9.2: 实现菜单可见性过滤逻辑
  - [x] SubTask 9.3: 在 HeaderViewModel 中应用权限过滤

## 阶段四：安全增强

- [x] Task 10: 实现密码安全机制
  - [x] SubTask 10.1: 创建 IPasswordHasher 服务接口
  - [x] SubTask 10.2: 实现 BCrypt/SM4 密码哈希
  - [x] SubTask 10.3: 重构登录验证使用哈希验证
  - [x] SubTask 10.4: 重构密码修改功能

## 阶段五：通知系统

- [x] Task 11: 实现通知系统基础功能
  - [x] SubTask 11.1: 创建通知实体模型
  - [x] SubTask 11.2: 创建 INotificationService 接口
  - [x] SubTask 11.3: 实现 NotificationService 服务
  - [x] SubTask 11.4: 完善 AppNotificationView 界面

## 阶段六：组织架构管理

- [x] Task 12: 实现组织架构管理
  - [x] SubTask 12.1: 创建组织架构管理视图
  - [x] SubTask 12.2: 实现组织树形结构显示
  - [x] SubTask 12.3: 实现用户-组织关联管理

# Task Dependencies

- Task 2 依赖 Task 1
- Task 3 依赖 Task 1
- Task 4 可独立进行
- Task 5 依赖 Task 2, Task 4
- Task 6 依赖 Task 3, Task 4
- Task 7 依赖 Task 3, Task 4
- Task 8 依赖 Task 3
- Task 9 依赖 Task 8
- Task 10 可独立进行
- Task 11 可独立进行
- Task 12 依赖 Task 1, Task 2
