using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeeKingdom.Shared.Serialization;

public static class BeeJson
{
    public static JsonSerializerOptions CreateDefaultOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }
}
