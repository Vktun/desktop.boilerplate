# 目标实现与演进方向（Roadmap）

本仓库的定位不是"带 WPF Shell 的管理系统演示"，而是**上位机二次开发平台**。当前状态与目标的权威差距分析见 `docs/PROJECT_REVIEW_AND_TODO.md`（评审于 2026-06-03；其中个别文件路径已过时，以代码为准）。

## 目标架构（五层）

1. **宿主 Shell** — WPF Shell、导航、主题、通知、对话框、状态栏、布局
2. **通用平台服务** — 账户、权限、配置、日志、审计、设置、文件存储、更新服务
3. **工业运行时内核** — 设备、协议、点位、轮询调度、命令回写、告警引擎、历史数据库
4. **项目模块** — 成型、搅拌、车间、质量、报表、配方、维护
5. **项目应用** — 客户特定启动组合与品牌定制

## 当前主要差距（做改动时的方向约束）

**P0（工业核心）阶段一+二已落地（2026-10）**：工业运行时内核以独立模块 `prismModules/Vk.Dbp.DeviceModule`（AccountModule 同款形态）落地——设备/点位/命令/历史四表领域模型、轮询采集引擎（故障退避重连 + 告警边沿联动 + 节流事件 + 配置变更 reconcile 热生效）、内置模拟驱动 + vktun.iot.connector（Vktun.IoT.Connector）Modbus TCP/RTU 真实驱动、主库 PointHistory 历史表（保留策略清理）、"实时监控"页（ScottPlot 趋势）、"设备管理"工程配置页（设备/点位/命令增删改 + 命令回写执行按钮，写侧服务带审计与级联删除）。**仍缺**：OPC UA/MQTT 驱动、报表中心、趋势历史查询页。

- 新增此类工业域抽象时：**契约放 `Vk.Dbp.Contracts`，通用实现放 `Vk.Dbp.Services` 或独立新模块，绝不放进 shell（WpfWindow）**。引擎内部契约（驱动/实时仓/历史/目录）留在 DeviceModule，跨模块消费出现时再提升 Contracts（AccountModule 契约提升同款演进）。
- 协议适配：Modbus TCP/RTU 已接（vktun.iot.connector，点位地址语法 `HR/IR/C/DI:地址[:类型]`）；规划中的 OPC UA、MQTT 在 DeviceModule 驱动工厂加分支。

**P1（已知架构债，勿扩大）**：

- 菜单权限双写：`MenuPermissionConfig` 静态类 + 数据库两份数据，有漂移风险——改权限相关功能时避免加深双写。
- `UserEditDialogViewModel` 用 `Action<bool>` 回调而非 DI，不符合项目约定；新增对话框 VM 不要模仿它。
- `Vk.Dbp.Tests.Integration` 为空；账户/权限/持久化的集成测试是待办。
- 阶段 B 未完成项：`IUpdateService`、文件存储服务。

**P2（工程品质）**：Nullable 警告存量、部分事件订阅 ViewModel 未实现 `IDisposable`、覆盖率门禁（≥40%）已配置未强制。

## 对日常任务的含义

- 接到模糊需求时，优先判断它属于五层中的哪一层，再决定放置位置（配合 `/dbp-new-module` skill 的放置决策表）。
- 改造现有代码时保持向目标架构收敛的趋势；至少不新增与目标方向冲突的依赖（如模块互依、shell 长业务逻辑）。
