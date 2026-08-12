namespace BeeKingdom.Shared.Serialization;

public sealed class SystemTextJsonContractSerializer : IContractSerializer
{
    public string Serialize<T>(T value)
    {
        return System.Text.Json.JsonSerializer.Serialize(value, BeeJson.CreateDefaultOptions());
    }

    public T? Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, BeeJson.CreateDefaultOptions());
    }
}
