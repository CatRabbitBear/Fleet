using Fleet.Blazor.PluginSystem.Dtos;
using ModelContextProtocol.Client;

namespace Fleet.Blazor.PluginSystem.Interfaces;

public interface IPluginManager
{
    IReadOnlyList<McpManifest> ListAvailableServers();
    McpManifest? GetManifest(string idOrName);
    Task<IMcpClient> AcquireClientAsync(string idOrName);
    Task ReleaseClientAsync(string id, bool dispose = false);
    Task<IMcpClient> CreateClientFromManifest(McpManifest manifest);
    ValueTask DisposeAsync();
}
