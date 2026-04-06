# 贡献指南

感谢你对 Desktop Boilerplate 项目的兴趣！本文档提供贡献指南，帮助您参与项目开发。

## 📋 目录

- [行为准则](#行为准则)
- [如何贡献](#如何贡献)
- [开发环境设置](#开发环境设置)
- [提交Pull Request](#提交pull-request)
- [代码审查流程](#代码审查流程)
- [编码规范](#编码规范)

---

## 行为准则

本项目采用 [Contributor Covenant](https://www.contributor-covenant.org/) 行为准则。参与本项目的所有贡献者需遵守以下原则：

- 尊重所有参与者
- 接受建设性批评
- 关注对社区最有利的事情
- 对其他社区成员表现出同理心

不当行为将被警告，严重者将被禁止参与项目。

---

## 如何贡献

### 报告Bug

如果您发现了Bug，请按以下步骤操作：

1. **搜索现有Issues** - 确保Bug未被报告
2. **使用Bug Report模板** - 提供详细信息：
   - Bug描述
   - 复现步骤
   - 期望行为
   - 实际行为
   - 环境信息（OS、.NET版本等）
   - 截图或日志

**Bug Report模板：**

```markdown
## Bug描述
简要描述Bug

## 复现步骤
1. 打开应用
2. 点击登录
3. 输入用户名和密码
4. 看到错误

## 期望行为
登录成功并跳转到Dashboard

## 实际行为
显示"数据库连接失败"错误

## 环境信息
- OS: Windows 11
- .NET: 10.0.100
- 应用版本: 0.2.0

## 日志
```
[Error] Database connection failed...
```

## 截图
（如有）
```

### 提出新功能

如果您有新功能建议：

1. **先开Issue讨论** - 说明功能价值和使用场景
2. **等待反馈** - 维护者会评估可行性
3. **达成共识后开始开发**

**Feature Request模板：**

```markdown
## 功能描述
添加用户头像上传功能

## 使用场景
用户希望个性化自己的账户，上传头像可以：
1. 提升用户体验
2. 在用户列表中更容易识别用户
3. 增强系统的社交属性

## 建议实现
- 使用Azure Blob Storage存储图片
- 支持JPG、PNG格式
- 最大文件大小2MB
- 自动生成缩略图

## 替代方案
使用Gravatar第三方服务
```

### 提交代码

我们欢迎代码贡献！请遵循以下流程：

```bash
# 1. Fork项目
git clone https://github.com/yourusername/desktop.boilerplate.git
cd desktop.boilerplate

# 2. 创建特性分支
git checkout -b feature/amazing-feature

# 3. 进行更改并测试
dotnet build
dotnet test

# 4. 提交更改（遵循Conventional Commits）
git commit -m "feat: add user avatar upload feature"

# 5. 推送到分支
git push origin feature/amazing-feature

# 6. 创建Pull Request
```

---

## 开发环境设置

### 必需工具

- **.NET 10 SDK** - [下载地址](https://dotnet.microsoft.com/download)
- **Visual Studio 2022+** 或 **JetBrains Rider** - 推荐IDE
- **Git** - 版本控制
- **SQL Server 2019+** - 数据库（开发可用LocalDB）

### 克隆和构建

```bash
# 克隆仓库
git clone https://github.com/yourorg/desktop.boilerplate.git
cd desktop.boilerplate

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 运行应用
dotnet run --project src/Vk.Dbp.WpfWindow
```

### 项目配置

1. 创建本地配置文件：
   ```bash
   cd src/Vk.Dbp.WpfWindow
   cp appsettings.local.example.json appsettings.local.json
   ```

2. 修改数据库连接字符串

3. 运行应用，数据库将自动初始化

---

## 提交Pull Request

### PR检查清单

提交PR前，请确保：

- [ ] 代码通过所有测试
- [ ] 新增代码有单元测试
- [ ] 更新了相关文档
- [ ] 遵循编码规范
- [ ] Commit消息符合规范

### PR模板

```markdown
## 描述
添加用户头像上传功能，支持JPG/PNG格式，最大2MB

## 变更类型
- [ ] Bug修复
- [x] 新功能
- [ ] 重构
- [ ] 文档更新
- [ ] 测试相关

## 测试
1. 上传JPG图片 - 成功
2. 上传PNG图片 - 成功
3. 上传超过2MB图片 - 显示错误提示
4. 上传不支持格式 - 显示错误提示

## 截图
（如有UI变更，提供截图）

## 相关问题
Closes #123

## 检查清单
- [x] 代码通过所有测试
- [x] 新增单元测试
- [x] 更新API文档
- [x] 遵循编码规范
```

---

## 代码审查流程

### 审查标准

我们会审查以下方面：

1. **功能正确性** - 代码是否实现了预期功能
2. **代码质量** - 可读性、可维护性、性能
3. **测试覆盖** - 是否有足够的测试
4. **文档完整性** - API文档、用户文档
5. **安全性** - 是否存在安全漏洞

### 审查流程

```
提交PR → 自动CI检查 → 代码审查 → 反馈/批准 → 合并
```

1. **自动CI检查** - GitHub Actions运行测试和代码质量检查
2. **代码审查** - 至少1名维护者审查
3. **修改请求** - 根据反馈修改代码
4. **批准** - 审查通过后批准
5. **合并** - Squash merge到main分支

### 审查时间

我们会在 **3个工作日内** 初步审查您的PR。如果超过这个时间没有回复，请@维护者。

---

## 编码规范

### C#规范

- **缩进**: 使用4空格缩进
- **大括号**: Allman风格（大括号另起一行）
- **命名约定**:
  - 类、方法、属性: PascalCase
  - 私有字段: `_camelCase`
  - 局部变量、参数: camelCase
  - 常量: PascalCase 或 SCREAMING_SNAKE_CASE
- **访问修饰符**: 显式声明（public、private等）
- **异步方法**: 使用Async后缀
- **事件**: 使用EventHandler<T>模式

**代码示例：**

```csharp
public class UserService : IUserService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<UserService> _logger;
    
    public UserService(ISqlSugarClient db, ILogger<UserService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        try
        {
            return await _db.Queryable<User>()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by id {UserId}", userId);
            throw;
        }
    }
}
```

### XAML规范

- **元素属性**: 按字母顺序排列
- **资源引用**: 使用DynamicResource（支持主题切换）
- **样式定义**: 在ResourceDictionary中定义
- **命名**: 使用x:Name属性，遵循camelCase

**XAML示例：**

```xml
<UserControl x:Class="Vk.Dbp.AccountModule.Views.UserView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:hc="https://handyorg.github.io/handycontrol">
    
    <UserControl.Resources>
        <ResourceDictionary>
            <Style x:Key="PrimaryButton" TargetType="Button">
                <Setter Property="Background" Value="{DynamicResource PrimaryBrush}"/>
                <Setter Property="Foreground" Value="White"/>
            </Style>
        </ResourceDictionary>
    </UserControl.Resources>
    
    <Grid>
        <Button x:name="saveButton"
                Content="保存"
                Style="{StaticResource PrimaryButton}"/>
    </Grid>
</UserControl>
```

### Git规范

#### 分支命名

```
feature/功能名称     # 新功能
fix/bug描述          # Bug修复
docs/文档类型        # 文档更新
refactor/重构内容    # 重构
test/测试类型        # 测试相关
```

#### Commit消息格式

遵循 [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**类型（type）：**

- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档变更
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具链变更
- `perf`: 性能优化
- `style`: 代码风格（不影响功能）

**示例：**

```
feat(account): add password strength meter

- Display password strength indicator
- Add validation rules
- Update UI with real-time feedback

Closes #123
```

---

## 发布流程

### 版本号规则

遵循 [语义化版本](https://semver.org/lang/zh-CN/):

```
MAJOR.MINOR.PATCH

MAJOR: 不兼容的API变更
MINOR: 向后兼容的功能新增
PATCH: 向后兼容的问题修复
```

### 发布步骤

1. **更新版本号** - 修改 `common.props` 和 `CHANGELOG.md`
2. **创建Git Tag** - `git tag v1.0.0`
3. **推送Tag** - `git push origin v1.0.0`
4. **GitHub Release** - 自动生成Release Notes
5. **发布NuGet包** - 可选

---

## 获取帮助

- **文档**: 查看项目 `docs/` 目录
- **Issues**: 在GitHub Issues提问
- **讨论**: 使用GitHub Discussions

---

## 许可证

通过贡献代码，您同意您的代码将根据MIT许可证授权。

---

**感谢您的贡献！** 🎉