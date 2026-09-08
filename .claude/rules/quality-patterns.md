---
paths:
  - "**/*.cs"
---

# C# 质量模式（CodeQL 加固约定）

改动任何 C# 文件时，完成前对照以下模式自检。这些模式来自 GitHub CodeQL 对本仓库的真实告警沉淀（见 `.trae/rules/development-workflow.md` 与提交 4d690db、ef80f8a）。

## 1. 可空值解构：用模式匹配，不用 `.Value`

lambda、查询谓词、回调里禁止 `HasValue` 之后跟 `.Value`；用非空模式捕获局部变量：

```csharp
// 错误
if (filter.AlarmLevel.HasValue)
    query = query.Where(a => a.Level == filter.AlarmLevel.Value);

// 正确
if (filter.AlarmLevel is { } alarmLevel)
    query = query.Where(a => a.Level == alarmLevel);
```

## 2. `readonly` 与 getter-only

- 仅在构造函数赋值的字段标 `readonly`；仅初始化赋值的公共集合用 getter-only。
- **例外**：`SetProperty(ref _field, value)` 的绑定后备字段必须保持可变（`ref` 参数不能是 readonly）。CodeQL 对此误报时，保留可变并选择最小改写。

## 3. 投影用 `Select`，副作用保留 `foreach`

- 一对一无副作用投影：`var names = users.Select(u => u.Username).ToList();`
- 循环体含事件订阅、命令执行、UI 变更、日志或多个语句时，保留 `foreach`（更清晰）。

## 4. 合并可合并的嵌套 `if`

所有条件必须同时成立且无有价值的中间分支时，合并为单一守卫：

```csharp
// 合并前
if (user != null)
{
    if (user.IsActive)
    {
        ...
    }
}

// 合并后
if (user is { IsActive: true })
{
    ...
}
```

## 5. 禁止局部变量遮蔽

局部变量不得遮蔽字段、属性、参数或有意义的外层作用域名。CodeQL 告警时重命名局部变量，而不是加限定符绕过。

## 数据操作异常过滤约定

数据库/IO 相关代码统一使用仓库既有过滤器，不要新造异常类型判断：

```csharp
try
{
    // 数据操作
}
catch (Exception ex) when (ExpectedOperationExceptionFilter.IsExpectedDataOperationException(ex))
{
    // 记录日志 / Growl 提示 / 审计失败
}
```

- 数据操作：`Dabp.Utils.Exceptions.ExpectedOperationExceptionFilter.IsExpectedDataOperationException`
- 文件操作：`IsExpectedFileOperationException`；模块内可加薄封装（如 AccountModule 的 `AccountOperationExceptionFilter`）。
- 不要捕获裸 `Exception` 后静默吞掉——要么过滤特定预期异常，要么让意外异常上抛。
