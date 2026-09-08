# Git 提交卫生

## Commit message

- Conventional commits 格式：`feat(account): ...`、`fix(export): ...`、`refactor(core): ...`（破坏性变更用 `refactor!:`）。中英文均可，仓库现状混用。
- **禁止 message 以 ``` 或其它代码围栏字符开头**——历史提交中多次出现 `` ``` feat(account): ... `` 残留事故，提交前检查 message 首字符。
- 一行说清意图；关联的模块/层写在 scope 里。

## 永不提交

- `appsettings.local.json`、任何真实连接串/密钥/令牌/生产密码（SM4 密钥同理）。
- `bin/`、`obj/`、日志文件、发布产物（`publish.ps1` 输出目录）。
- 临时/种子密码。开发默认账号 admin / 123456 只能出现在文档说明中。

## 改动纪律

- 不回滚用户手动做出的改动；`git status` 里发现用户未提交的脏文件时，在交付说明中点名而不是清理它们。
- 无关格式化改动不混入功能/修复 commit。
- 纯 markdown/rules/skills 改动验证方式：`git diff --check`。
