using Fleet.Blazor.PluginSystem.Dtos;
using Fleet.Blazor.PluginSystem.Interfaces;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using Quickenshtein;

namespace Fleet.Blazor.PluginSystem;

public class McpPluginManager : IPluginManager, IAsyncDisposable
{
    private readonly ILogger<McpPluginManager> _logger;
    private readonly IMcpRepoManager _repoManager;
    private readonly McpServerPool _pool = new();
    private List<McpManifest>? _manifests;

    public McpPluginManager(IMcpRepoManager repoManager, ILogger<McpPluginManager> logger)
    {
        _repoManager = repoManager;
        _logger = logger;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_manifests != null) return;
        var manifests = await _repoManager.GetAllMcpServersAsync();
        _manifests = manifests.ToList();
    }

    public IReadOnlyList<McpManifest> ListAvailableServers()
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        return _manifests!.AsReadOnly();
    }

    private McpManifest LookupManifest(string input, Func<McpManifest, string> keySelector, int maxDistance = 3)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        var exact = _manifests!.FirstOrDefault(m =>
            string.Equals(keySelector(m), input, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        var best = _manifests!
            .Select(m => new
            {
                Manifest = m,
                Distance = Levenshtein.GetDistance(input, keySelector(m))
            })
            .OrderBy(x => x.Distance)
            .First();

        if (best.Distance <= maxDistance)
            return best.Manifest;

        _logger.LogWarning(
            "No exact match for '{Input}' found. Closest match '{BestMatch}' with distance {Distance} exceeds threshold {MaxDistance}.",
            input, best.Manifest.Id, best.Distance, maxDistance);
        throw new KeyNotFoundException($"No manifest matching '{input}' and no close fuzzy match found.");
    }

    public McpManifest? GetManifest(string idOrName)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        var list = _manifests!;
        return list.FirstOrDefault(m =>
                   string.Equals(m.Id, idOrName, StringComparison.OrdinalIgnoreCase))
               ?? list.FirstOrDefault(m =>
                   string.Equals(m.Name, idOrName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IMcpClient> AcquireClientAsync(string idOrName)
    {
        await EnsureLoadedAsync();
        var manifest = _manifests!.Any(m =>
                string.Equals(m.Id, idOrName, StringComparison.OrdinalIgnoreCase))
            ? LookupManifest(idOrName, m => m.Id)
            : LookupManifest(idOrName, m => m.Name);
        return await _pool.AcquireAsync(manifest.Id, () => CreateClientFromManifest(manifest));
    }

    public Task ReleaseClientAsync(string id, bool dispose = false) => _pool.ReleaseAsync(id, dispose);

    public Task<IMcpClient> CreateClientFromManifest(McpManifest m)
    {
        var opts = new StdioClientTransportOptions
        {
            Name = m.Id,
            Command = m.Runtime ?? "npx",
            Arguments = m.Args?.ToArray() ?? Array.Empty<string>(),
            EnvironmentVariables = m.Env?.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value) ?? new Dictionary<string, string?>(),
        };
        return McpClientFactory.CreateAsync(new StdioClientTransport(opts));
    }

    public async ValueTask DisposeAsync() => await _pool.DisposeAsync();
}

