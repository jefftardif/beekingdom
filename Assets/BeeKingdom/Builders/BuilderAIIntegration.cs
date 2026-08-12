using System;
using System.Collections.Generic;
using BeeKingdom.Buildings;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Builders
{
    public enum BuilderWorkState { Idle, TaskAssigned, TravelToSite, WaitingResources, Building, Break, TaskCompleted, Interrupted, Reassigned, EmergencyRecall }

    public sealed class BuilderProfile
    {
        public string BuilderId { get; }
        public double DistanceToSite { get; private set; }
        public double Experience { get; private set; }
        public double Fatigue { get; private set; }
        public bool Available { get; private set; }

        public BuilderProfile(string builderId, double distanceToSite, double experience, double fatigue, bool available = true)
        {
            BuilderId = string.IsNullOrWhiteSpace(builderId) ? throw new ArgumentException("Builder id is required.", nameof(builderId)) : builderId;
            DistanceToSite = distanceToSite < 0d ? 0d : distanceToSite;
            Experience = experience < 0d ? 0d : experience;
            Fatigue = fatigue < 0d ? 0d : fatigue;
            Available = available;
        }

        public void SetAvailable(bool available) => Available = available;
    }

    public sealed class BuilderWorkSession
    {
        public string BuilderId { get; }
        public string ConstructionInstanceId { get; private set; }
        public BuilderWorkState State { get; private set; }
        public double WorkContributed { get; private set; }

        public BuilderWorkSession(string builderId)
        {
            BuilderId = builderId ?? string.Empty;
            State = BuilderWorkState.Idle;
        }

        public void Assign(string constructionInstanceId)
        {
            ConstructionInstanceId = constructionInstanceId ?? string.Empty;
            State = BuilderWorkState.TaskAssigned;
        }

        public void Arrive() => State = BuilderWorkState.TravelToSite;
        public void StartWork() => State = BuilderWorkState.Building;
        public void Pause() => State = BuilderWorkState.Interrupted;
        public void Resume() => State = BuilderWorkState.Building;
        public void Reassign(string constructionInstanceId) { ConstructionInstanceId = constructionInstanceId ?? string.Empty; State = BuilderWorkState.Reassigned; }
        public void Complete() => State = BuilderWorkState.TaskCompleted;

        public void AddContribution(double amount)
        {
            if (State == BuilderWorkState.Building && amount > 0d) WorkContributed += amount;
        }
    }

    public sealed class BuilderReservationManager
    {
        private readonly HashSet<string> reservedBuilders = new HashSet<string>();
        public int ReservedCount => reservedBuilders.Count;
        public bool Reserve(string builderId) => reservedBuilders.Add(builderId);
        public bool Release(string builderId) => reservedBuilders.Remove(builderId);
        public bool IsReserved(string builderId) => reservedBuilders.Contains(builderId);
    }

    public sealed class BuilderAssignmentEngine
    {
        public IReadOnlyList<BuilderProfile> SelectBuilders(IReadOnlyList<BuilderProfile> candidates, int requestedCount, int priority)
        {
            List<BuilderProfile> available = new List<BuilderProfile>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Available) available.Add(candidates[i]);
            }

            available.Sort((left, right) =>
            {
                int scoreCompare = Score(right, priority).CompareTo(Score(left, priority));
                return scoreCompare != 0 ? scoreCompare : string.CompareOrdinal(left.BuilderId, right.BuilderId);
            });

            int count = Math.Min(Math.Max(0, requestedCount), available.Count);
            return available.GetRange(0, count);
        }

        private static double Score(BuilderProfile builder, int priority)
        {
            return priority * 1000d + builder.Experience * 10d - builder.Fatigue * 5d - builder.DistanceToSite;
        }
    }

    public sealed class BuilderTaskDispatcher
    {
        public void Dispatch(BuilderWorkSession session)
        {
            session.Arrive();
            session.StartWork();
        }
    }

    public sealed class BuilderIntegrationDiagnostics
    {
        public int Assigned { get; private set; }
        public int Released { get; private set; }
        public int Reassigned { get; private set; }
        public int Interrupted { get; private set; }
        public int Resumed { get; private set; }
        public double TotalContribution { get; private set; }

        public void RecordAssigned(int count) => Assigned += count;
        public void RecordReleased(int count) => Released += count;
        public void RecordReassigned() => Reassigned++;
        public void RecordInterrupted() => Interrupted++;
        public void RecordResumed() => Resumed++;
        public void RecordContribution(double contribution) => TotalContribution += contribution;
    }

    public sealed class BuilderIntegrationManager
    {
        private readonly Dictionary<string, BuilderProfile> builders = new Dictionary<string, BuilderProfile>();
        private readonly Dictionary<string, BuilderWorkSession> sessionsByBuilder = new Dictionary<string, BuilderWorkSession>();
        private readonly BuilderAssignmentEngine assignmentEngine = new BuilderAssignmentEngine();
        private readonly BuilderReservationManager reservationManager = new BuilderReservationManager();
        private readonly BuilderTaskDispatcher dispatcher = new BuilderTaskDispatcher();
        private readonly ConstructionWorkflowManager workflowManager;
        private readonly IEventBus eventBus;

        public BuilderIntegrationDiagnostics Diagnostics { get; } = new BuilderIntegrationDiagnostics();

        public BuilderIntegrationManager(ConstructionWorkflowManager workflowManager = null, IEventBus eventBus = null)
        {
            this.workflowManager = workflowManager;
            this.eventBus = eventBus;
        }

        public bool RegisterBuilder(BuilderProfile profile)
        {
            if (profile == null || builders.ContainsKey(profile.BuilderId)) return false;
            builders.Add(profile.BuilderId, profile);
            sessionsByBuilder.Add(profile.BuilderId, new BuilderWorkSession(profile.BuilderId));
            return true;
        }

        public IReadOnlyList<BuilderWorkSession> AssignBuilders(string constructionInstanceId, int requestedCount, int priority)
        {
            List<BuilderProfile> candidates = new List<BuilderProfile>(builders.Values);
            IReadOnlyList<BuilderProfile> selected = assignmentEngine.SelectBuilders(candidates, requestedCount, priority);
            List<BuilderWorkSession> assigned = new List<BuilderWorkSession>();

            for (int i = 0; i < selected.Count; i++)
            {
                BuilderProfile profile = selected[i];
                if (!reservationManager.Reserve(profile.BuilderId)) continue;
                profile.SetAvailable(false);
                BuilderWorkSession session = sessionsByBuilder[profile.BuilderId];
                session.Assign(constructionInstanceId);
                dispatcher.Dispatch(session);
                assigned.Add(session);
                eventBus?.Publish(new BuilderAssigned(profile.BuilderId, constructionInstanceId));
                eventBus?.Publish(new BuilderArrived(profile.BuilderId, constructionInstanceId));
                eventBus?.Publish(new BuilderStartedWorking(profile.BuilderId, constructionInstanceId));
            }

            Diagnostics.RecordAssigned(assigned.Count);
            return assigned;
        }

        public int ReleaseBuilders(string constructionInstanceId)
        {
            int released = 0;
            foreach (BuilderWorkSession session in sessionsByBuilder.Values)
            {
                if (session.ConstructionInstanceId != constructionInstanceId) continue;
                session.Complete();
                builders[session.BuilderId].SetAvailable(true);
                reservationManager.Release(session.BuilderId);
                released++;
                eventBus?.Publish(new BuilderReleased(session.BuilderId, constructionInstanceId));
            }

            Diagnostics.RecordReleased(released);
            return released;
        }

        public bool ReassignBuilder(string builderId, string constructionInstanceId)
        {
            if (!sessionsByBuilder.TryGetValue(builderId, out BuilderWorkSession session)) return false;
            session.Reassign(constructionInstanceId);
            session.StartWork();
            Diagnostics.RecordReassigned();
            eventBus?.Publish(new BuilderReassigned(builderId, constructionInstanceId));
            return true;
        }

        public bool PauseBuilderWork(string builderId)
        {
            if (!sessionsByBuilder.TryGetValue(builderId, out BuilderWorkSession session)) return false;
            session.Pause();
            Diagnostics.RecordInterrupted();
            eventBus?.Publish(new BuilderStoppedWorking(builderId, session.ConstructionInstanceId));
            return true;
        }

        public bool ResumeBuilderWork(string builderId)
        {
            if (!sessionsByBuilder.TryGetValue(builderId, out BuilderWorkSession session)) return false;
            session.Resume();
            Diagnostics.RecordResumed();
            eventBus?.Publish(new BuilderStartedWorking(builderId, session.ConstructionInstanceId));
            return true;
        }

        public IReadOnlyList<BuilderWorkSession> QueryAssignedBuilders(string constructionInstanceId)
        {
            List<BuilderWorkSession> result = new List<BuilderWorkSession>();
            foreach (BuilderWorkSession session in sessionsByBuilder.Values)
            {
                if (session.ConstructionInstanceId == constructionInstanceId && reservationManager.IsReserved(session.BuilderId)) result.Add(session);
            }
            result.Sort((left, right) => string.CompareOrdinal(left.BuilderId, right.BuilderId));
            return result;
        }

        public double CalculateWorkContribution(string constructionInstanceId, double deltaSeconds)
        {
            double total = 0d;
            IReadOnlyList<BuilderWorkSession> sessions = QueryAssignedBuilders(constructionInstanceId);
            for (int i = 0; i < sessions.Count; i++)
            {
                BuilderProfile profile = builders[sessions[i].BuilderId];
                double contribution = Math.Max(0d, deltaSeconds) * (1d + profile.Experience * 0.1d) * Math.Max(0.1d, 1d - profile.Fatigue);
                sessions[i].AddContribution(contribution);
                total += contribution;
            }

            if (workflowManager != null && total > 0d)
            {
                workflowManager.AdvanceConstruction(constructionInstanceId, total, 1d);
            }

            Diagnostics.RecordContribution(total);
            eventBus?.Publish(new BuilderContributionUpdated(constructionInstanceId, total));
            return total;
        }
    }

    public readonly struct BuilderAssigned : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderAssigned(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderArrived : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderArrived(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderStartedWorking : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderStartedWorking(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderStoppedWorking : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderStoppedWorking(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderReassigned : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderReassigned(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderReleased : IGameplayEvent, IBuildingEvent { public string BuilderId { get; } public string ConstructionId { get; } public BuilderReleased(string builderId, string constructionId) { BuilderId = builderId; ConstructionId = constructionId; } }
    public readonly struct BuilderContributionUpdated : IGameplayEvent, IBuildingEvent { public string ConstructionId { get; } public double Contribution { get; } public BuilderContributionUpdated(string constructionId, double contribution) { ConstructionId = constructionId; Contribution = contribution; } }
}
