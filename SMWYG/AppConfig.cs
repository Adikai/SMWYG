using System.Text.Json;

namespace SMWYG
{
    public static class AppConfig
    {
        public static JsonElement Root { get; private set; }

        public static void Load(string path)
        {
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, path));
            Root = JsonSerializer.Deserialize<JsonElement>(json);
        }

        public static string? GetString(string path)
        {
            var parts = path.Split(':');
            JsonElement current = Root;
            foreach (var p in parts)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(p, out current))
                    return null;
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
        }
    }
}
