using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.WorldMap;

public static class WorldMapChunkJson
{
    public static JsonSerializerOptions CreateOptions(bool writeIndented = false)
    {
        JsonSerializerOptions options = BeeJson.CreateDefaultOptions();
        options.WriteIndented = writeIndented;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.Converters.Add(new WorldIdJsonConverter());
        options.Converters.Add(new GameServerIdJsonConverter());
        options.Converters.Add(new ContractVersionJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed class WorldIdJsonConverter : JsonConverter<WorldId>
    {
        public override WorldId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new WorldId(ReadGuid(ref reader, nameof(WorldId)));
        }

        public override void Write(Utf8JsonWriter writer, WorldId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value.ToString("N"));
        }
    }

    private sealed class GameServerIdJsonConverter : JsonConverter<GameServerId>
    {
        public override GameServerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new GameServerId(ReadGuid(ref reader, nameof(GameServerId)));
        }

        public override void Write(Utf8JsonWriter writer, GameServerId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value.ToString("N"));
        }
    }

    private sealed class ContractVersionJsonConverter : JsonConverter<ContractVersion>
    {
        public override ContractVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("ContractVersion must be a canonical string.");
            }

            string? value = reader.GetString();
            string[] segments = value?.Split('.', StringSplitOptions.None) ?? [];
            if (segments.Length != 3
                || !int.TryParse(segments[0], NumberStyles.None, CultureInfo.InvariantCulture, out int major)
                || !int.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minor)
                || !int.TryParse(segments[2], NumberStyles.None, CultureInfo.InvariantCulture, out int patch))
            {
                throw new JsonException("ContractVersion must use the canonical major.minor.patch format.");
            }

            string canonical = FormattableString.Invariant($"{major}.{minor}.{patch}");
            if (!string.Equals(value, canonical, StringComparison.Ordinal))
            {
                throw new JsonException("ContractVersion must use the canonical major.minor.patch format.");
            }

            return new ContractVersion(major, minor, patch);
        }

        public override void Write(Utf8JsonWriter writer, ContractVersion value, JsonSerializerOptions options)
        {
            if (value.Major < 0 || value.Minor < 0 || value.Patch < 0)
            {
                throw new JsonException("ContractVersion components cannot be negative.");
            }

            writer.WriteStringValue(FormattableString.Invariant($"{value.Major}.{value.Minor}.{value.Patch}"));
        }
    }

    private static Guid ReadGuid(ref Utf8JsonReader reader, string typeName)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeName} must be a canonical 32-character GUID string.");
        }

        string? value = reader.GetString();
        if (value is null
            || !Guid.TryParseExact(value, "N", out Guid parsed)
            || !string.Equals(value, parsed.ToString("N"), StringComparison.Ordinal))
        {
            throw new JsonException($"{typeName} must be a lowercase 32-character GUID string.");
        }

        return parsed;
    }
}
