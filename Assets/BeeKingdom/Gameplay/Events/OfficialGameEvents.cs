using System;

namespace BeeKingdom.Gameplay.Events
{
    public readonly struct BuildingStarted : IGameEvent
    {
        public string BuildingId { get; }
        public string Source { get; }
        public BuildingStarted(string buildingId, string source = "construction") { BuildingId = buildingId ?? string.Empty; Source = source ?? string.Empty; }
    }

    public readonly struct BuildingCompleted : IGameEvent
    {
        public string BuildingId { get; }
        public Guid OperationId { get; }
        public string Source { get; }
        public BuildingCompleted(string buildingId, Guid operationId, string source = "construction") { BuildingId = buildingId ?? string.Empty; OperationId = operationId; Source = source ?? string.Empty; }
    }

    public readonly struct ResearchStarted : IGameEvent
    {
        public string ResearchId { get; }
        public string Source { get; }
        public ResearchStarted(string researchId, string source = "research") { ResearchId = researchId ?? string.Empty; Source = source ?? string.Empty; }
    }

    public readonly struct ResearchCompleted : IGameEvent
    {
        public string ResearchId { get; }
        public string Source { get; }
        public ResearchCompleted(string researchId, string source = "research") { ResearchId = researchId ?? string.Empty; Source = source ?? string.Empty; }
    }

    public readonly struct SpeedUpUsed : IGameEvent
    {
        public string ItemId { get; }
        public long DurationSeconds { get; }
        public SpeedUpUsed(string itemId, long durationSeconds) { ItemId = itemId ?? string.Empty; DurationSeconds = durationSeconds; }
    }

    public readonly struct RewardGranted : IGameEvent
    {
        public string RewardId { get; }
        public long Amount { get; }
        public string Source { get; }
        public RewardGranted(string rewardId, long amount, string source) { RewardId = rewardId ?? string.Empty; Amount = amount; Source = source ?? string.Empty; }
    }
}
