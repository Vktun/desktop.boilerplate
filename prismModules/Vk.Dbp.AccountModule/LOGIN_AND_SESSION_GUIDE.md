# 用户登录和会话管理系统

## 概述

这个系统提供了完整的用户登录、会话管理和注销功能。核心是 `UserSession` 单例类，用于保存当前登录用户的信息和状态。

## 核心组件

### 1. UserSession（用户会话类）
**位置**: `prismModules\Vk.Dbp.AccountModule\Models\UserSession.cs`

这是一个单例类，用于保存当前登录用户的信息：

```csharp
// 获取会话实例
var userSession = UserSession.Instance;

// 检查用户是否登录
if (userSession.IsLoggedIn)
{
    // 获取用户信息
    var userId = userSession.UserId;
    var username = userSession.Username;
    var realName = userSession.RealName;
    var email = userSession.Email;
    var phone = userSession.Phone;
}
```

### 2. 登录界面
**位置**: `prismModules\Vk.Dbp.AccountModule\Views\LoginView.xaml`

提供用户登录界面，包括：
- 用户名输入框
- 密码输入框
- 记住密码选项
- 错误消息显示
- 加载指示器

### 3. 登录ViewModel
**位置**: `prismModules\Vk.Dbp.AccountModule\ViewModels\LoginViewModel.cs`

处理登录逻辑：
- 验证用户凭证
- 检查用户是否被禁用
- 保存用户会话信息
- 导航到主页面或显示错误

## 使用流程

### 应用启动
1. 应用启动时，`Bootstrapper` 会检查 `UserSession.IsLoggedIn`
2. 如果未登录，导航到 `LoginView`
3. 如果已登录，导航到 `Dashboard`

### 用户登录
1. 用户在 `LoginView` 输入用户名和密码
2. 点击"登录"按钮
3. `LoginViewModel` 验证凭证：
   - 检查用户是否存在
   - 检查用户是否被启用
   - 验证密码
4. 登录成功：
   - 保存用户信息到 `UserSession`
   - 导航到主应用界面
   - `HeaderView` 自动更新显示用户名和状态
5. 登录失败：
   - 显示错误消息

### 显示用户信息
`HeaderView` 会自动显示：
- 用户名（从 `UserSession.RealName` 或 `UserSession.Username`）
- 用户状态（在线/离线）
- 用户菜单（修改密码、注销、关机等）

### 用户注销
1. 点击账户菜单的"注销"按钮
2. `HeaderViewModel` 调用 `UserSession.Logout()`
3. 清除所有用户信息
4. 导航回到 `LoginView`

## 相关代码文件

### 核心文件
- `Models/UserSession.cs` - 用户会话管理类
- `Views/LoginView.xaml` - 登录界面
- `ViewModels/LoginViewModel.cs` - 登录逻辑

### 相关文件
- `src/Vk.Dbp.WpfWindow/ViewModels/HeaderViewModel.cs` - 显示用户信息和处理注销
- `src/Vk.Dbp.WpfWindow/Layout/HeaderView.xaml` - 用户界面显示
- `src/Vk.Dbp.WpfWindow/PrismBootstrapper.cs` - 应用启动配置

## API 参考

### UserSession 类

#### 属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | `static UserSession` | 获取单例实例 |
| `UserId` | `int` | 用户ID |
| `Username` | `string` | 用户名 |
| `RealName` | `string` | 真实姓名 |
| `Email` | `string` | 邮箱 |
| `Phone` | `string` | 电话 |
| `IsLoggedIn` | `bool` | 是否已登录 |
| `LoginTime` | `DateTime` | 登录时间 |
| `Token` | `string` | 认证令牌 |

#### 方法
| 方法 | 说明 |
|------|------|
| `Login(userId, username, realName, email, phone, token)` | 登录用户 |
| `Login(User user, token)` | 使用User对象登录 |
| `Logout()` | 注销用户 |
| `Clear()` | 清空用户信息 |
| `ResetInstance()` | 重置单例（用于测试） |

## 默认测试用户

为了方便测试，系统预设了默认用户：
- **用户名**: admin
- **密码**: 123456

这些凭证可在 `UserService` 类中找到的示例数据中修改。

## 密码处理

当前实现使用简化的密码验证方式。对于生产环境，应该：
1. 对密码进行哈希处理
2. 使用加密存储密码
3. 实现安全的密码验证

修改 `LoginViewModel` 中的 `ValidatePassword` 方法以实现更安全的验证。

## 注意事项

1. **线程安全**: `UserSession` 使用锁机制确保线程安全
2. **单例模式**: 整个应用共享同一个 `UserSession` 实例
3. **事件订阅**: `HeaderViewModel` 自动订阅 `UserSession` 的属性变更事件，实时更新UI
4. **密码框**: 密码框内容通过命令参数传递，不直接绑定以确保安全

## 扩展和自定义

### 添加新的登录信息字段
1. 在 `UserSession` 中添加属性
2. 在 `User` 模型中添加对应字段
3. 在 `LoginViewModel` 中更新 `Login` 方法

### 集成实际身份认证服务
1. 创建 `IAuthenticationService` 接口
2. 实现与实际认证服务的集成
3. 在 `LoginViewModel` 中调用认证服务而不是 `UserService`

### 自定义登录界面
- 修改 `LoginView.xaml` 的XAML布局和样式
- 在 `LoginViewModel` 中修改验证逻辑
