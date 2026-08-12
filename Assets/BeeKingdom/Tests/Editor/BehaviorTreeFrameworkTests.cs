using BeeKingdom.AI;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BehaviorTreeFrameworkTests
    {
        [Test]
        public void SequenceTreeCompletes()
        {
            BehaviorTreeManager manager = CreateManager();
            BehaviorTreeInstance instance = manager.ExecuteTree("build-tree", "bee-1");

            Assert.That(manager.TickTree(instance.InstanceId, 1d), Is.EqualTo(BehaviorNodeState.Success));
        }

        [Test]
        public void InterruptAndResumeTree()
        {
            BehaviorTreeManager manager = CreateManager();
            BehaviorTreeInstance instance = manager.ExecuteTree("build-tree", "bee-1");

            Assert.That(manager.InterruptTree(instance.InstanceId), Is.True);
            Assert.That(manager.QueryTreeState(instance.InstanceId), Is.EqualTo(BehaviorNodeState.Interrupted));
            Assert.That(manager.ResumeTree(instance.InstanceId), Is.True);
        }

        [Test]
        public void BlackboardStoresValues()
        {
            BehaviorTreeManager manager = CreateManager();
            BehaviorTreeInstance instance = manager.ExecuteTree("build-tree", "bee-1");

            instance.Blackboard.Set("target", "cell-1");

            Assert.That(instance.Blackboard.TryGet("target", out string target), Is.True);
            Assert.That(target, Is.EqualTo("cell-1"));
        }

        private static BehaviorTreeManager CreateManager()
        {
            BehaviorNode root = new BehaviorNode("root", BehaviorNodeType.Root);
            BehaviorNode sequence = new BehaviorNode("sequence", BehaviorNodeType.Sequence);
            sequence.AddChild(new BehaviorNode("move", BehaviorNodeType.Action));
            sequence.AddChild(new BehaviorNode("build", BehaviorNodeType.Action));
            root.AddChild(sequence);
            BehaviorTreeManager manager = new BehaviorTreeManager();
            manager.RegisterBehaviorTree(new BehaviorTreeDefinition("build-tree", root));
            return manager;
        }
    }
}
