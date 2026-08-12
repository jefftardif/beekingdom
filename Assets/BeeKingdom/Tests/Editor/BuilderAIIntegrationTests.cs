using BeeKingdom.Builders;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class BuilderAIIntegrationTests
    {
        [Test]
        public void AssignBuildersSelectsBestAvailable()
        {
            BuilderIntegrationManager manager = CreateManager();

            var sessions = manager.AssignBuilders("construction", 2, 1);

            Assert.That(sessions.Count, Is.EqualTo(2));
            Assert.That(sessions[0].State, Is.EqualTo(BuilderWorkState.Building));
            Assert.That(manager.QueryAssignedBuilders("construction").Count, Is.EqualTo(2));
        }

        [Test]
        public void WorkContributionIsCollaborative()
        {
            BuilderIntegrationManager manager = CreateManager();
            manager.AssignBuilders("construction", 2, 1);

            double contribution = manager.CalculateWorkContribution("construction", 10d);

            Assert.That(contribution, Is.GreaterThan(10d));
            Assert.That(manager.Diagnostics.TotalContribution, Is.EqualTo(contribution));
        }

        [Test]
        public void PauseResumeReassignAndReleaseBuilders()
        {
            BuilderIntegrationManager manager = CreateManager();
            manager.AssignBuilders("construction", 1, 1);
            BuilderWorkSession session = manager.QueryAssignedBuilders("construction")[0];

            Assert.That(manager.PauseBuilderWork(session.BuilderId), Is.True);
            Assert.That(session.State, Is.EqualTo(BuilderWorkState.Interrupted));
            Assert.That(manager.ResumeBuilderWork(session.BuilderId), Is.True);
            Assert.That(manager.ReassignBuilder(session.BuilderId, "other"), Is.True);
            Assert.That(manager.ReleaseBuilders("other"), Is.EqualTo(1));
            Assert.That(manager.QueryAssignedBuilders("other").Count, Is.EqualTo(0));
        }

        [Test]
        public void AssignmentIsDeterministicByScoreThenId()
        {
            BuilderIntegrationManager manager = new BuilderIntegrationManager();
            manager.RegisterBuilder(new BuilderProfile("b", 1d, 1d, 0d));
            manager.RegisterBuilder(new BuilderProfile("a", 1d, 1d, 0d));

            var sessions = manager.AssignBuilders("construction", 1, 1);

            Assert.That(sessions[0].BuilderId, Is.EqualTo("a"));
        }

        private static BuilderIntegrationManager CreateManager()
        {
            BuilderIntegrationManager manager = new BuilderIntegrationManager();
            manager.RegisterBuilder(new BuilderProfile("near-expert", 1d, 5d, 0.1d));
            manager.RegisterBuilder(new BuilderProfile("far-new", 10d, 0d, 0.1d));
            manager.RegisterBuilder(new BuilderProfile("rested", 3d, 2d, 0d));
            return manager;
        }
    }
}
