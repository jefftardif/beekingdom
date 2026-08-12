using System;
using System.Collections.Generic;
using BeeKingdom.Core.Logging;
using BeeKingdom.Core.Services;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ServiceLifecycleTests
    {
        [Test]
        public void BootstrapUsesDependencyBeforePriority()
        {
            List<string> calls = new List<string>();
            ServiceLifecycleOrchestrator orchestrator = CreateOrchestrator(out ServiceContainer container);

            orchestrator.Register<IFirstService>(new RecordedService("first", 100, calls));
            orchestrator.Register<ISecondService>(new RecordedService("second", 10, calls, typeof(IFirstService)));

            orchestrator.Bootstrap();

            Assert.That(calls.IndexOf("first.Initialize"), Is.LessThan(calls.IndexOf("second.Initialize")));
            Assert.That(calls.IndexOf("first.Start"), Is.LessThan(calls.IndexOf("second.Start")));
            Assert.That(container.TryGet(out IFirstService _), Is.True);
        }

        [Test]
        public void ShutdownRunsInReverseStartupOrder()
        {
            List<string> calls = new List<string>();
            ServiceLifecycleOrchestrator orchestrator = CreateOrchestrator(out _);

            orchestrator.Register<IFirstService>(new RecordedService("first", 10, calls));
            orchestrator.Register<ISecondService>(new RecordedService("second", 20, calls, typeof(IFirstService)));

            orchestrator.Bootstrap();
            orchestrator.Shutdown();

            Assert.That(calls.IndexOf("second.Shutdown"), Is.LessThan(calls.IndexOf("first.Shutdown")));
        }

        [Test]
        public void MissingDependencyFailsBeforeStartup()
        {
            ServiceLifecycleOrchestrator orchestrator = CreateOrchestrator(out _);
            orchestrator.Register<ISecondService>(new RecordedService("second", 10, new List<string>(), typeof(IFirstService)));

            Assert.Throws<InvalidOperationException>(() => orchestrator.Bootstrap());
        }

        [Test]
        public void CircularDependencyIsDetected()
        {
            ServiceLifecycleOrchestrator orchestrator = CreateOrchestrator(out _);
            orchestrator.Register<IFirstService>(new RecordedService("first", 10, new List<string>(), typeof(ISecondService)));
            orchestrator.Register<ISecondService>(new RecordedService("second", 20, new List<string>(), typeof(IFirstService)));

            Assert.Throws<InvalidOperationException>(() => orchestrator.Bootstrap());
        }

        [Test]
        public void FailedServicePreventsDependentFromStarting()
        {
            ServiceLifecycleOrchestrator orchestrator = CreateOrchestrator(out _);
            FailingService failing = new FailingService();
            RecordedService dependent = new RecordedService("dependent", 20, new List<string>(), typeof(IFirstService));

            orchestrator.Register<IFirstService>(failing);
            orchestrator.Register<ISecondService>(dependent);

            orchestrator.Bootstrap();

            Assert.That(failing.State, Is.EqualTo(ServiceState.Failed));
            Assert.That(dependent.State, Is.EqualTo(ServiceState.Failed));
        }

        private static ServiceLifecycleOrchestrator CreateOrchestrator(out ServiceContainer container)
        {
            container = new ServiceContainer();
            return new ServiceLifecycleOrchestrator(container, new NullLogger());
        }

        private interface IFirstService
        {
        }

        private interface ISecondService
        {
        }

        private sealed class RecordedService : GameServiceBase, IFirstService, ISecondService
        {
            private readonly string label;
            private readonly List<string> calls;
            private readonly IReadOnlyList<Type> dependencies;

            public override int Priority { get; }
            public override IReadOnlyList<Type> Dependencies => dependencies;

            public RecordedService(string label, int priority, List<string> calls, params Type[] dependencies)
            {
                this.label = label;
                Priority = priority;
                this.calls = calls;
                this.dependencies = dependencies;
            }

            protected override void OnInitialize(IServiceRegistry services)
            {
                calls.Add($"{label}.Initialize");
            }

            protected override void OnStart()
            {
                calls.Add($"{label}.Start");
            }

            protected override void OnShutdown()
            {
                calls.Add($"{label}.Shutdown");
            }
        }

        private sealed class FailingService : GameServiceBase, IFirstService
        {
            protected override void OnInitialize(IServiceRegistry services)
            {
                throw new InvalidOperationException("Expected test failure.");
            }
        }

        private sealed class NullLogger : IBeeLogger
        {
            public BeeLogLevel MinimumLevel { get; set; }
            public void Log(BeeLogLevel level, string message)
            {
            }
        }
    }
}
