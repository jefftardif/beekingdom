namespace BeeKingdom.Shared.Serialization;

public interface IContractSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string payload);
}
