using Fleet.Blazor.PluginSystem.Dtos;

namespace Fleet.Blazor.PluginSystem.Interfaces;

public interface IMcpRepoManager
{
    Task<IReadOnlyList<McpManifest>> GetAllMcpServersAsync();
    Task<McpManifest?> GetMcpServerByNameAsync(string idOrName);
}
