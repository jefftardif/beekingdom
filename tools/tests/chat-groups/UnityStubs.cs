// Harness .NET uniquement : aucune equivalence avec les API natives Unity garantie.
namespace UnityEngine
{
    public static class Debug
    {
        public static void LogError(object value) => System.Console.Error.WriteLine(value);
        public static void LogWarning(object value) => System.Console.Error.WriteLine(value);
    }

    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options =
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true };

        public static string ToJson(object value) =>
            System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), Options);

        public static T FromJson<T>(string json) =>
            System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
    }
}

namespace BeeKingdom.Gameplay.Communication
{
    // Contrat normalement declare dans UnityWebRequestChatRestTransport.cs, exclu ici.
    public interface IChatJsonCodec
    {
        string Serialize(object value);
        T Deserialize<T>(string json);
    }
}
