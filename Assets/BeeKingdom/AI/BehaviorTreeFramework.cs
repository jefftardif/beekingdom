using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.AI
{
    public enum BehaviorNodeType { Root, Sequence, Selector, Parallel, Condition, Action, Wait, Repeat, Retry, Decorator, RandomSelector, PrioritySelector, Cooldown, Timeout }
    public enum BehaviorNodeState { Ready, Running, Success, Failure, Interrupted, Cancelled }

    public sealed class BehaviorBlackboard
    {
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        public void Set(string key, object value) => values[key ?? string.Empty] = value;
        public bool TryGet<T>(string key, out T value)
        {
            if (values.TryGetValue(key ?? string.Empty, out object stored) && stored is T typed) { value = typed; return true; }
            value = default;
            return false;
        }
    }

    public sealed class BehaviorNode
    {
        private readonly List<BehaviorNode> children = new List<BehaviorNode>();
        public string NodeId { get; }
        public BehaviorNodeType Type { get; }
        public double DurationSeconds { get; }
        public BehaviorNodeState State { get; private set; }
        public IReadOnlyList<BehaviorNode> Children => children;

        public BehaviorNode(string nodeId, BehaviorNodeType type, double durationSeconds = 0d)
        {
            NodeId = string.IsNullOrWhiteSpace(nodeId) ? throw new ArgumentException("Node id is required.", nameof(nodeId)) : nodeId;
            Type = type;
            DurationSeconds = Math.Max(0d, durationSeconds);
            State = BehaviorNodeState.Ready;
        }

        public void AddChild(BehaviorNode child) { if (child != null) children.Add(child); }
        public void SetState(BehaviorNodeState state) => State = state;
    }

    public sealed class BehaviorTreeDefinition
    {
        public string TreeId { get; }
        public BehaviorNode Root { get; }
        public BehaviorTreeDefinition(string treeId, BehaviorNode root)
        {
            TreeId = string.IsNullOrWhiteSpace(treeId) ? throw new ArgumentException("Tree id is required.", nameof(treeId)) : treeId;
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }
    }

    public sealed class BehaviorTreeInstance
    {
        public string InstanceId { get; }
        public string TreeId { get; }
        public string BeeId { get; }
        public BehaviorNodeState State { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public BehaviorBlackboard Blackboard { get; } = new BehaviorBlackboard();

        public BehaviorTreeInstance(string instanceId, string treeId, string beeId)
        {
            InstanceId = instanceId;
            TreeId = treeId;
            BeeId = beeId ?? string.Empty;
            State = BehaviorNodeState.Ready;
        }

        public void SetState(BehaviorNodeState state) => State = state;
        public void Advance(double deltaSeconds) => ElapsedSeconds += Math.Max(0d, deltaSeconds);
    }

    public sealed class BehaviorTreeEngine
    {
        public BehaviorNodeState Tick(BehaviorNode node)
        {
            node.SetState(BehaviorNodeState.Running);
            switch (node.Type)
            {
                case BehaviorNodeType.Sequence:
                case BehaviorNodeType.Root:
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        BehaviorNodeState state = Tick(node.Children[i]);
                        if (state != BehaviorNodeState.Success) { node.SetState(state); return state; }
                    }
                    node.SetState(BehaviorNodeState.Success);
                    return BehaviorNodeState.Success;
                case BehaviorNodeType.Selector:
                case BehaviorNodeType.PrioritySelector:
                case BehaviorNodeType.RandomSelector:
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        if (Tick(node.Children[i]) == BehaviorNodeState.Success) { node.SetState(BehaviorNodeState.Success); return BehaviorNodeState.Success; }
                    }
                    node.SetState(BehaviorNodeState.Failure);
                    return BehaviorNodeState.Failure;
                case BehaviorNodeType.Parallel:
                    bool anyFailure = false;
                    for (int i = 0; i < node.Children.Count; i++) anyFailure |= Tick(node.Children[i]) == BehaviorNodeState.Failure;
                    node.SetState(anyFailure ? BehaviorNodeState.Failure : BehaviorNodeState.Success);
                    return node.State;
                default:
                    node.SetState(BehaviorNodeState.Success);
                    return BehaviorNodeState.Success;
            }
        }
    }

    public sealed class BehaviorTreeDiagnostics
    {
        public int Registered { get; private set; }
        public int Started { get; private set; }
        public int Completed { get; private set; }
        public int Failed { get; private set; }
        public int Interrupted { get; private set; }
        public void RecordRegistered() => Registered++;
        public void RecordStarted() => Started++;
        public void RecordCompleted() => Completed++;
        public void RecordFailed() => Failed++;
        public void RecordInterrupted() => Interrupted++;
    }

    public sealed class BehaviorTreeManager
    {
        private readonly Dictionary<string, BehaviorTreeDefinition> definitions = new Dictionary<string, BehaviorTreeDefinition>();
        private readonly Dictionary<string, BehaviorTreeInstance> instances = new Dictionary<string, BehaviorTreeInstance>();
        private readonly BehaviorTreeEngine engine = new BehaviorTreeEngine();
        private readonly IEventBus eventBus;
        private int sequence;

        public BehaviorTreeDiagnostics Diagnostics { get; } = new BehaviorTreeDiagnostics();
        public BehaviorTreeManager(IEventBus eventBus = null) { this.eventBus = eventBus; }

        public bool RegisterBehaviorTree(BehaviorTreeDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.TreeId)) return false;
            definitions.Add(definition.TreeId, definition);
            Diagnostics.RecordRegistered();
            return true;
        }

        public BehaviorTreeInstance ExecuteTree(string treeId, string beeId)
        {
            if (!definitions.ContainsKey(treeId)) return null;
            BehaviorTreeInstance instance = new BehaviorTreeInstance("tree-" + (++sequence).ToString("D6"), treeId, beeId);
            instance.SetState(BehaviorNodeState.Running);
            instances.Add(instance.InstanceId, instance);
            Diagnostics.RecordStarted();
            eventBus?.Publish(new BehaviorTreeStarted(instance.InstanceId));
            return instance;
        }

        public BehaviorNodeState TickTree(string instanceId, double deltaSeconds)
        {
            if (!instances.TryGetValue(instanceId, out BehaviorTreeInstance instance) || !definitions.TryGetValue(instance.TreeId, out BehaviorTreeDefinition definition)) return BehaviorNodeState.Failure;
            instance.Advance(deltaSeconds);
            BehaviorNodeState state = engine.Tick(definition.Root);
            instance.SetState(state);
            if (state == BehaviorNodeState.Success) { Diagnostics.RecordCompleted(); eventBus?.Publish(new BehaviorTreeCompleted(instanceId)); }
            if (state == BehaviorNodeState.Failure) { Diagnostics.RecordFailed(); eventBus?.Publish(new BehaviorTreeFailed(instanceId)); }
            return state;
        }

        public bool InterruptTree(string instanceId) => SetTreeState(instanceId, BehaviorNodeState.Interrupted, () => { Diagnostics.RecordInterrupted(); eventBus?.Publish(new TreeInterrupted(instanceId)); });
        public bool ResumeTree(string instanceId) => SetTreeState(instanceId, BehaviorNodeState.Running, null);
        public bool AbortTree(string instanceId) => SetTreeState(instanceId, BehaviorNodeState.Cancelled, null);
        public BehaviorNodeState QueryTreeState(string instanceId) => instances.TryGetValue(instanceId, out BehaviorTreeInstance instance) ? instance.State : BehaviorNodeState.Failure;

        private bool SetTreeState(string instanceId, BehaviorNodeState state, Action publish)
        {
            if (!instances.TryGetValue(instanceId, out BehaviorTreeInstance instance)) return false;
            instance.SetState(state);
            publish?.Invoke();
            return true;
        }
    }

    public readonly struct BehaviorTreeStarted : IBeeEvent { public string InstanceId { get; } public BehaviorTreeStarted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct BehaviorTreeCompleted : IBeeEvent { public string InstanceId { get; } public BehaviorTreeCompleted(string instanceId) { InstanceId = instanceId; } }
    public readonly struct BehaviorTreeFailed : IBeeEvent { public string InstanceId { get; } public BehaviorTreeFailed(string instanceId) { InstanceId = instanceId; } }
    public readonly struct NodeStarted : IBeeEvent { public string NodeId { get; } public NodeStarted(string nodeId) { NodeId = nodeId; } }
    public readonly struct NodeCompleted : IBeeEvent { public string NodeId { get; } public NodeCompleted(string nodeId) { NodeId = nodeId; } }
    public readonly struct NodeFailed : IBeeEvent { public string NodeId { get; } public NodeFailed(string nodeId) { NodeId = nodeId; } }
    public readonly struct TreeInterrupted : IBeeEvent { public string InstanceId { get; } public TreeInterrupted(string instanceId) { InstanceId = instanceId; } }
}
