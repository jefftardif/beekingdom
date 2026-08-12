using BeeKingdom.Protocol;
using BeeKingdom.Protocol.Errors;
using BeeKingdom.Protocol.Messages;
using BeeKingdom.Protocol.Requests;
using BeeKingdom.Protocol.Responses;
using BeeKingdom.Protocol.Validation;
using BeeKingdom.Protocol.Versioning;
using BeeKingdom.Shared.ValueObjects;
using System.Text.Json;

namespace BeeKingdom.Tests;

public sealed class ProtocolTests
{
    [Test]
    public void ProtocolMessageContainsRequiredEnvelopeFields()
    {
        ProtocolMessage<PingRequest> message = CreatePingMessage();

        Assert.Multiple(() =>
        {
            Assert.That(message.ProtocolVersion, Is.EqualTo(ProtocolVersion.Current));
            Assert.That(message.MessageId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.MessageType, Is.EqualTo(ProtocolMessageType.Request));
            Assert.That(message.CorrelationId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.TraceId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.SessionId, Is.EqualTo("session-1"));
            Assert.That(message.PlayerId.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.ColonyId.Value, Is.Not.EqualTo(Guid.Empty));
            Assert.That(message.Payload, Is.Not.Null);
        });
    }

    [Test]
    public void ProtocolManagerSerializesAndDeserializesMessage()
    {
        ProtocolManager manager = new();
        ProtocolMessage<PingRequest> message = CreatePingMessage();

        byte[] bytes = manager.Serialize(message);
        ProtocolMessage<PingRequest>? restored = manager.Deserialize<PingRequest>(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(restored, Is.EqualTo(message));
            Assert.That(manager.Diagnostics.MessageCount, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.BytesSerialized, Is.GreaterThan(0));
            Assert.That(manager.Diagnostics.BytesDeserialized, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void ValidationRejectsMissingSession()
    {
        ProtocolManager manager = new();
        ProtocolMessage<PingRequest> message = CreatePingMessage() with { SessionId = "" };

        ProtocolValidationResult result = manager.Validate(message, 128);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ProtocolErrorCode.Unauthorized));
        });
    }

    [Test]
    public void VersionNegotiationSelectsSupportedVersion()
    {
        ProtocolManager manager = new();

        ProtocolVersion selected = manager.NegotiateVersion([new ProtocolVersion(2, 0), new ProtocolVersion(1, 0)]);

        Assert.That(selected, Is.EqualTo(ProtocolVersion.Current));
    }

    [Test]
    public void VersionNegotiationRecordsUnsupportedVersionError()
    {
        ProtocolManager manager = new();

        ProtocolVersion selected = manager.NegotiateVersion([new ProtocolVersion(2, 0)]);

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.EqualTo(default(ProtocolVersion)));
            Assert.That(manager.Diagnostics.ErrorCount, Is.EqualTo(1));
            Assert.That(manager.Diagnostics.ErrorsByCode[ProtocolErrorCode.UnsupportedVersion], Is.EqualTo(1));
        });
    }

    [Test]
    public void PlayableHiveLoopReadinessContractRemainsReadOnlyNonLive()
    {
        PlayableHiveLoopReadinessResponse response = new(
            "BeeKingdom.Server",
            DateTimeOffset.UnixEpoch,
            "Development",
            "00000000-0000-0000-0000-000000000001",
            "00000000-0000-0000-0000-000000000101",
            "FutureContractOnly",
            ReadOnly: true,
            NonLive: true,
            OfficialEndpoint: false,
            MutationAllowed: false,
            PersistenceClaimAllowed: false,
            RealTimeSynchronizationEnabled: false,
            new PlayableHiveLoopReadinessModel(
                [new PlayerResourceReadModel("honey", "Honey", Amount: null, Capacity: null, ServerAuthoritative: false, Live: false)],
                [new PlayerBuildingReadModel("storage", "Storage", Level: null, "PreviewOnly", UpgradeAvailable: false, ServerAuthoritative: false, Live: false)],
                [new BuildingLevelReadModel("storage", 1, [new ResourceCostReadModel("honey", Amount: null, ServerAuthoritative: false, Live: false)], ConstructionSeconds: null, ServerAuthoritative: false, Live: false)],
                [new BuildingUpgradeReadModel("storage", FromLevel: null, ToLevel: null, "ReadOnlyPreview", [], DurationSeconds: null, MutationAllowed: false, ServerAuthoritative: false, Live: false)],
                [new ConstructionQueueReadModel("slot-1", BuildingKey: null, "EmptyPreview", StartedAtUtc: null, CompletesAtUtc: null, ServerAuthoritative: false, Live: false)],
                [new PlayerTroopReadModel("worker-bee", "Worker Bee", Count: null, Level: null, ServerAuthoritative: false, Live: false)],
                [new TroopTrainingReadModel("training-slot-1", TroopKey: null, Quantity: null, "EmptyPreview", StartedAtUtc: null, CompletesAtUtc: null, MutationAllowed: false, ServerAuthoritative: false, Live: false)],
                new PlayerArmyReadModel(TotalTroops: null, Capacity: null, AssignedTroops: null, AvailableTroops: null, ServerAuthoritative: false, Live: false)),
            new PlayableHiveLoopForbiddenClaims(
                OfficialEndpoint: true,
                OfficialResources: true,
                OfficialBuildings: true,
                OfficialBuildingLevels: true,
                OfficialBuildingUpgrades: true,
                OfficialConstructionQueue: true,
                OfficialTroops: true,
                OfficialTraining: true,
                OfficialArmy: true,
                OfficialProgression: true,
                OfficialPersistence: true,
                RealTimeSynchronization: true),
            ["No official endpoint is defined for this future contract."]);

        string payload = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        JsonElement readModel = root.GetProperty("readModel");
        JsonElement forbiddenClaims = root.GetProperty("forbiddenClaims");

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("readOnly").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("nonLive").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("officialEndpoint").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("persistenceClaimAllowed").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("realTimeSynchronizationEnabled").GetBoolean(), Is.False);
            Assert.That(readModel.GetProperty("playerResources")[0].GetProperty("amount").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(readModel.GetProperty("buildings")[0].GetProperty("level").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(readModel.GetProperty("buildingUpgrades")[0].GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(readModel.GetProperty("constructionQueue")[0].GetProperty("completesAtUtc").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(readModel.GetProperty("troops")[0].GetProperty("count").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(readModel.GetProperty("training")[0].GetProperty("mutationAllowed").GetBoolean(), Is.False);
            Assert.That(readModel.GetProperty("army").GetProperty("totalTroops").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(forbiddenClaims.GetProperty("officialResources").GetBoolean(), Is.True);
            Assert.That(forbiddenClaims.GetProperty("officialBuildings").GetBoolean(), Is.True);
            Assert.That(forbiddenClaims.GetProperty("officialTraining").GetBoolean(), Is.True);
            Assert.That(forbiddenClaims.GetProperty("realTimeSynchronization").GetBoolean(), Is.True);
            Assert.That(payload, Does.Not.Contain("endpointPath"));
            Assert.That(payload, Does.Not.Contain("accountId"));
            Assert.That(payload, Does.Not.Contain("sessionId"));
        });
    }

    private static ProtocolMessage<PingRequest> CreatePingMessage()
    {
        return ProtocolMessage<PingRequest>.Create(
            ProtocolMessageType.Request,
            "session-1",
            PlayerId.New(),
            ColonyId.New(),
            new PingRequest("tests", DateTimeOffset.UnixEpoch));
    }
}
