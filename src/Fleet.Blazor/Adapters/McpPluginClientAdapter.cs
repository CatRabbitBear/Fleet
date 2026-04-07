using Fleet.Blazor.PluginSystem;
using Fleet.Runtime.Adapters;

namespace Fleet.Blazor.Adapters;

/// <summary>
/// Blazor host adapter for runtime plugin client lifecycle operations.
/// </summary>
public class McpPluginClientAdapter : IPluginClientAdapter
{
    private readonly McpPluginManager _pluginManager;

    public McpPluginClientAdapter(McpPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<object> AcquireClientAsync(string idOrName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _pluginManager.AcquireClientAsync(idOrName);
    }

    public async Task ReleaseClientAsync(string idOrName, bool dispose = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _pluginManager.ReleaseClientAsync(idOrName, dispose);
    }
}
