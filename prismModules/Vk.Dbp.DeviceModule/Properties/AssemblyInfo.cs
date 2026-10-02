using System.Runtime.CompilerServices;

// 单元测试需要访问引擎内部类型（AlarmEdgeDetector 状态机、SimulatedSignal 解析器）
[assembly: InternalsVisibleTo("Vk.Dbp.Tests.Unit")]
