using BeeKingdom.Core.Events;
using BeeKingdom.Population;

namespace BeeKingdom.AI
{
    public readonly struct BeeTaskStarted : IBeeEvent { public string BeeId { get; } public string TaskId { get; } public BeeTaskStarted(string beeId, string taskId) { BeeId = beeId; TaskId = taskId; } }
    public readonly struct BeeTaskCompleted : IBeeEvent { public string BeeId { get; } public string TaskId { get; } public BeeTaskCompleted(string beeId, string taskId) { BeeId = beeId; TaskId = taskId; } }
    public readonly struct BeeStateChanged : IBeeEvent { public string BeeId { get; } public BeeBehaviorState State { get; } public BeeStateChanged(string beeId, BeeBehaviorState state) { BeeId = beeId; State = state; } }
    public readonly struct BeeBehaviorInterrupted : IBeeEvent { public string BeeId { get; } public BeeBehaviorInterrupted(string beeId) { BeeId = beeId; } }
    public readonly struct BeeWaiting : IBeeEvent { public string BeeId { get; } public BeeWaiting(string beeId) { BeeId = beeId; } }
    public readonly struct BeeIdle : IBeeEvent { public string BeeId { get; } public BeeIdle(string beeId) { BeeId = beeId; } }
    public readonly struct BehaviorStarted : IBeeEvent { public string BeeId { get; } public BeeIntent Intent { get; } public BehaviorStarted(string beeId, BeeIntent intent) { BeeId = beeId; Intent = intent; } }
    public readonly struct BehaviorCompleted : IBeeEvent { public string BeeId { get; } public BeeIntent Intent { get; } public BehaviorCompleted(string beeId, BeeIntent intent) { BeeId = beeId; Intent = intent; } }
    public readonly struct BehaviorInterrupted : IBeeEvent { public string BeeId { get; } public string Reason { get; } public BehaviorInterrupted(string beeId, string reason) { BeeId = beeId; Reason = reason; } }
    public readonly struct BehaviorFailed : IBeeEvent { public string BeeId { get; } public string Reason { get; } public BehaviorFailed(string beeId, string reason) { BeeId = beeId; Reason = reason; } }
    public readonly struct BeeResumed : IBeeEvent { public string BeeId { get; } public BeeResumed(string beeId) { BeeId = beeId; } }
}
