namespace Fleet.Runtime.Adapters;

/// <summary>
/// Host-owned adapter for acquiring and releasing plugin clients.
/// </summary>
public interface IPluginClientAdapter
{
    Task<object> AcquireClientAsync(string idOrName, CancellationToken cancellationToken = default);
    Task ReleaseClientAsync(string idOrName, bool dispose = false, CancellationToken cancellationToken = default);
}
