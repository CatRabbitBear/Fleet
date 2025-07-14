using Fleet.Blazor.PluginSystem.Dtos;
using System.Text.Json;

namespace Fleet.Blazor.PluginSystem.McpJson;

public static class PluginLoader
{
    public static List<McpManifest> LoadManifests(string folderPath)
    {
        var manifests = new List<McpManifest>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Plugin folder not found: {folderPath}");

        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var manifest = JsonSerializer.Deserialize<McpManifest>(json, options);
                if (manifest != null)
                    manifests.Add(manifest);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                Console.WriteLine($"Error loading plugin from {file}: {ex.Message}");
            }
        }

        return manifests;
    }
}
