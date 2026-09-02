using System.Text.Json;

namespace BeeKingdom.Alliance.Repositories;

// M042-CL: same atomic-write pattern as DurableJsonHiveStateRepository (temp file + File.Move)
// so a crash mid-write never leaves a half-written, corrupt JSON file behind. Shared by all four
// Alliance Durable repositories rather than duplicated four times.
internal static class DurableJsonFileIo
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    internal static void WriteAtomic(string path, object value)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temp = path + ".tmp." + Guid.NewGuid().ToString("N");
        File.WriteAllText(temp, JsonSerializer.Serialize(value, value.GetType(), Options));
        File.Move(temp, path, true);
    }

    internal static T? ReadIfExists<T>(string path)
    {
        if (!File.Exists(path)) return default;
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
    }

    internal static IEnumerable<string> EnumerateJsonFiles(string directory)
    {
        return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json") : Array.Empty<string>();
    }
}
