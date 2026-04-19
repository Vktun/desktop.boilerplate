using Prism.Events;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.Contracts.Events
{
    /// <summary>
    /// 告警触发事件 - 当新告警产生时发布
    /// </summary>
    public class AlarmTriggeredEvent : PubSubEvent<AlarmTriggeredPayload>
    {
    }

    /// <summary>
    /// 告警状态变更事件 - 当告警状态发生变更时发布
    /// </summary>
    public class AlarmStatusChangedEvent : PubSubEvent<AlarmStatusChangedPayload>
    {
    }

    /// <summary>
    /// 告警数量变更事件 - 当告警数量发生变化时发布（用于更新Badge）
    /// </summary>
    public class AlarmCountChangedEvent : PubSubEvent<AlarmCountChangedPayload>
    {
    }
}