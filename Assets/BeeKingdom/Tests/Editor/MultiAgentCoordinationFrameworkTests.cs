using BeeKingdom.Population;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class MultiAgentCoordinationFrameworkTests
    {
        [Test]
        public void TeamLifecycleWorks()
        {
            MultiAgentCoordinator coordinator = CreateCoordinator();
            TeamInstance team = coordinator.CreateTeam("construction", "bee-1", "build-cell");
            coordinator.JoinTeam(team.TeamId, "bee-2");
            coordinator.AssignRole(team.TeamId, "bee-2", TeamRole.Builder);

            Assert.That(coordinator.StartMission(team.TeamId), Is.True);
            Assert.That(coordinator.CompleteMission(team.TeamId), Is.True);
            Assert.That(coordinator.QueryTeams().Count, Is.EqualTo(0));
        }

        [Test]
        public void TeamWaitsUntilEnoughMembers()
        {
            MultiAgentCoordinator coordinator = CreateCoordinator();
            TeamInstance team = coordinator.CreateTeam("construction", "bee-1", "build-cell");

            Assert.That(team.State, Is.EqualTo(TeamState.WaitingMembers));
        }

        private static MultiAgentCoordinator CreateCoordinator()
        {
            MultiAgentCoordinator coordinator = new MultiAgentCoordinator();
            coordinator.RegisterPlan(new CoordinationPlan("construction", CooperationType.Construction, 2, 1));
            return coordinator;
        }
    }
}
