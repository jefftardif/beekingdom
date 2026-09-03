using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeeKingdom.Shared.ValueObjects;

// M043S-CL: PlayerIdJsonConverter (below) exists for the one place that actually needs it -
// CreateInvitationRequest.InvitedPlayerId, applied there directly via [JsonConverter] on that
// property, NOT here on the type. An earlier attempt attached this converter to PlayerId itself,
// which changed serialization everywhere PlayerId is touched by System.Text.Json (chat idempotency
// payload hashing, various in-process test fixtures, etc.) and broke 19 unrelated tests - PlayerId
// stays on its default record-struct shape globally; only the one broken call site opts in.
public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public sealed class PlayerIdJsonConverter : JsonConverter<PlayerId>
{
    public override PlayerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, PlayerId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Value.ToString("N"));
}

public readonly record struct ColonyId(Guid Value)
{
    public static ColonyId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct WorldId(Guid Value)
{
    public static WorldId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct GameServerId(Guid Value)
{
    public static GameServerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct BeeId(Guid Value)
{
    public static BeeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct BuildingId(Guid Value)
{
    public static BuildingId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct ChamberId(Guid Value)
{
    public static ChamberId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct AllianceId(Guid Value)
{
    public static AllianceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}
