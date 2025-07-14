using Fleet.Blazor.PluginSystem.Dtos;
using Fleet.Blazor.PluginSystem.Interfaces;
using Fleet.Blazor.SQLite;
using System.Text.Json;

namespace Fleet.Blazor.PluginSystem;

/// <summary>
/// Repository that loads MCP server manifests from a SQLite handler
/// returning all servers as a single JSON blob.
/// </summary>
public class McpJsonRepoManager : IMcpRepoManager
{
    private readonly SqliteMcpHandler _handler;

    public McpJsonRepoManager(SqliteMcpHandler handler)
    {
        _handler = handler;
    }

    public async Task<IReadOnlyList<McpManifest>> GetAllMcpServersAsync()
    {
        var json = await _handler.FetchAllMcpServers();
        var doc = JsonDocument.Parse(json);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var list = new List<McpManifest>();
        if (doc.RootElement.TryGetProperty("servers", out var servers))
        {
            foreach (var element in servers.EnumerateArray())
            {
                var manifest = element.Deserialize<McpManifest>(options);
                if (manifest != null)
                    list.Add(manifest);
            }
        }

        return list;
    }

    public async Task<McpManifest?> GetMcpServerByNameAsync(string idOrName)
    {
        var all = await GetAllMcpServersAsync();
        return all.FirstOrDefault(m =>
            string.Equals(m.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.Name, idOrName, StringComparison.OrdinalIgnoreCase));
    }
}