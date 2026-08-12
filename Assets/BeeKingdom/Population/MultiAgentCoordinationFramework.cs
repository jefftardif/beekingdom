using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum CooperationType { Construction, HeavyTransport, Defense, Repair, Exploration, Rescue, Cleaning, Ventilation, IntensiveHarvest, SpecialEvent, Custom }
    public enum TeamRole { Leader, Builder, Carrier, Defender, Scout, Healer, Support, Reserve }
    public enum TeamState { Forming, WaitingMembers, Ready, Executing, Suspended, Regrouping, Completed, Cancelled, Failed }

    public sealed class CoordinationPlan
    {
        public string PlanId { get; }
        public CooperationType Type { get; }
        public int RequiredMembers { get; }
        public int Priority { get; }
        public CoordinationPlan(string planId, CooperationType type, int requiredMembers, int priority)
        {
            PlanId = string.IsNullOrWhiteSpace(planId) ? throw new ArgumentException("Plan id is required.", nameof(planId)) : planId;
            Type = type;
            RequiredMembers = Math.Max(1, requiredMembers);
            Priority = priority;
        }
    }

    public sealed class TeamInstance
    {
        private readonly List<string> members = new List<string>();
        private readonly Dictionary<string, TeamRole> roles = new Dictionary<string, TeamRole>();
        public string TeamId { get; }
        public string Leader { get; private set; }
        public string Objective { get; }
        public int Priority { get; }
        public TeamState State { get; private set; }
        public double Progress { get; private set; }
        public IReadOnlyList<string> Members => members;
        public IReadOnlyDictionary<string, TeamRole> Roles => roles;

        public TeamInstance(string teamId, string leader, string objective, int priority)
        {
            TeamId = teamId;
            Leader = leader ?? string.Empty;
            Objective = objective ?? string.Empty;
            Priority = priority;
            State = TeamState.Forming;
            Join(Leader);
            AssignRole(Leader, TeamRole.Leader);
        }

        public bool Join(string beeId) { if (string.IsNullOrWhiteSpace(beeId) || members.Contains(beeId)) return false; members.Add(beeId); return true; }
        public bool Leave(string beeId) { roles.Remove(beeId); return members.Remove(beeId); }
        public void AssignRole(string beeId, TeamRole role) { if (members.Contains(beeId)) roles[beeId] = role; if (role == TeamRole.Leader) Leader = beeId; }
        public void SetState(TeamState state) => State = state;
        public void Advance(double amount) => Progress = Math.Min(1d, Progress + Math.Max(0d, amount));
    }

    public sealed class TeamManager
    {
        private readonly Dictionary<string, TeamInstance> teams = new Dictionary<string, TeamInstance>();
        public bool Register(TeamInstance team) { if (team == null || teams.ContainsKey(team.TeamId)) return false; teams.Add(team.TeamId, team); return true; }
        public bool TryGet(string teamId, out TeamInstance team) => teams.TryGetValue(teamId, out team);
        public void Remove(string teamId) => teams.Remove(teamId);
        public IReadOnlyList<TeamInstance> QueryTeams() { List<TeamInstance> result = new List<TeamInstance>(teams.Values); result.Sort((a, b) => string.CompareOrdinal(a.TeamId, b.TeamId)); return result; }
    }

    public sealed class CoordinationEngine
    {
        public bool IsReady(TeamInstance team, CoordinationPlan plan) => team.Members.Count >= plan.RequiredMembers;
        public void Synchronize(TeamInstance team, CoordinationPlan plan) => team.SetState(IsReady(team, plan) ? TeamState.Ready : TeamState.WaitingMembers);
    }

    public sealed class CoordinationDiagnostics
    {
        public int Created { get; private set; }
        public int Joined { get; private set; }
        public int Left { get; private set; }
        public int Started { get; private set; }
        public int Completed { get; private set; }
        public int Failed { get; private set; }
        public void RecordCreated() => Created++;
        public void RecordJoined() => Joined++;
        public void RecordLeft() => Left++;
        public void RecordStarted() => Started++;
        public void RecordCompleted() => Completed++;
        public void RecordFailed() => Failed++;
    }

    public sealed class MultiAgentCoordinator
    {
        private readonly Dictionary<string, CoordinationPlan> plans = new Dictionary<string, CoordinationPlan>();
        private readonly TeamManager teamManager = new TeamManager();
        private readonly CoordinationEngine engine = new CoordinationEngine();
        private readonly IEventBus eventBus;
        private int sequence;
        public CoordinationDiagnostics Diagnostics { get; } = new CoordinationDiagnostics();
        public MultiAgentCoordinator(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterPlan(CoordinationPlan plan) { if (plan == null || plans.ContainsKey(plan.PlanId)) return false; plans.Add(plan.PlanId, plan); return true; }

        public TeamInstance CreateTeam(string planId, string leaderBeeId, string objective)
        {
            if (!plans.TryGetValue(planId, out CoordinationPlan plan)) return null;
            TeamInstance team = new TeamInstance("team-" + (++sequence).ToString("D6"), leaderBeeId, objective, plan.Priority);
            teamManager.Register(team);
            engine.Synchronize(team, plan);
            Diagnostics.RecordCreated();
            eventBus?.Publish(new TeamCreated(team.TeamId));
            return team;
        }

        public bool JoinTeam(string teamId, string beeId)
        {
            if (!teamManager.TryGet(teamId, out TeamInstance team)) return false;
            bool joined = team.Join(beeId);
            if (joined) { Diagnostics.RecordJoined(); eventBus?.Publish(new BeeJoinedTeam(teamId, beeId)); }
            return joined;
        }

        public bool LeaveTeam(string teamId, string beeId)
        {
            if (!teamManager.TryGet(teamId, out TeamInstance team)) return false;
            bool left = team.Leave(beeId);
            if (left) { Diagnostics.RecordLeft(); eventBus?.Publish(new BeeLeftTeam(teamId, beeId)); }
            return left;
        }

        public bool AssignRole(string teamId, string beeId, TeamRole role) { if (!teamManager.TryGet(teamId, out TeamInstance team)) return false; team.AssignRole(beeId, role); return true; }
        public bool StartMission(string teamId) { if (!teamManager.TryGet(teamId, out TeamInstance team)) return false; team.SetState(TeamState.Executing); Diagnostics.RecordStarted(); eventBus?.Publish(new MissionStarted(teamId)); return true; }
        public bool CompleteMission(string teamId) { if (!teamManager.TryGet(teamId, out TeamInstance team)) return false; team.SetState(TeamState.Completed); Diagnostics.RecordCompleted(); eventBus?.Publish(new MissionCompleted(teamId)); eventBus?.Publish(new TeamDisbanded(teamId)); teamManager.Remove(teamId); return true; }
        public bool CancelMission(string teamId) { if (!teamManager.TryGet(teamId, out TeamInstance team)) return false; team.SetState(TeamState.Cancelled); eventBus?.Publish(new MissionFailed(teamId)); eventBus?.Publish(new TeamDisbanded(teamId)); teamManager.Remove(teamId); return true; }
        public IReadOnlyList<TeamInstance> QueryTeams() => teamManager.QueryTeams();
    }

    public readonly struct TeamCreated : IGameplayEvent, IBeeEvent { public string TeamId { get; } public TeamCreated(string teamId) { TeamId = teamId; } }
    public readonly struct BeeJoinedTeam : IGameplayEvent, IBeeEvent { public string TeamId { get; } public string BeeId { get; } public BeeJoinedTeam(string teamId, string beeId) { TeamId = teamId; BeeId = beeId; } }
    public readonly struct BeeLeftTeam : IGameplayEvent, IBeeEvent { public string TeamId { get; } public string BeeId { get; } public BeeLeftTeam(string teamId, string beeId) { TeamId = teamId; BeeId = beeId; } }
    public readonly struct TeamReady : IGameplayEvent, IBeeEvent { public string TeamId { get; } public TeamReady(string teamId) { TeamId = teamId; } }
    public readonly struct MissionStarted : IGameplayEvent, IBeeEvent { public string TeamId { get; } public MissionStarted(string teamId) { TeamId = teamId; } }
    public readonly struct MissionCompleted : IGameplayEvent, IBeeEvent { public string TeamId { get; } public MissionCompleted(string teamId) { TeamId = teamId; } }
    public readonly struct MissionFailed : IGameplayEvent, IBeeEvent { public string TeamId { get; } public MissionFailed(string teamId) { TeamId = teamId; } }
    public readonly struct TeamDisbanded : IGameplayEvent, IBeeEvent { public string TeamId { get; } public TeamDisbanded(string teamId) { TeamId = teamId; } }
}
