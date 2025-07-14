using ModelContextProtocol.Client;
using System.Collections.Concurrent;

namespace Fleet.Blazor.PluginSystem;

public class McpServerPool : IAsyncDisposable
{
    private class Entry
    {
        public IMcpClient Client { get; }
        private int _refCount;
        public int RefCount
        {
            get => _refCount;
            set => _refCount = value;
        }
        public Entry(IMcpClient client)
        {
            Client = client;
            _refCount = 1;
        }
        public void IncrementRefCount() => Interlocked.Increment(ref _refCount);
        public void DecrementRefCount() => Interlocked.Decrement(ref _refCount);
    }

    private readonly ConcurrentDictionary<string, Entry> _servers =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<IMcpClient> AcquireAsync(string serverKey, Func<Task<IMcpClient>> createClient)
    {
        if (_servers.TryGetValue(serverKey, out var entry))
        {
            entry.IncrementRefCount();
            return entry.Client;
        }

        var client = await createClient();
        entry = new Entry(client);
        _servers[serverKey] = entry;
        return client;
    }

    public async Task ReleaseAsync(string serverKey, bool disposeImmediately = false)
    {
        if (!_servers.TryGetValue(serverKey, out var entry))
            return;

        entry.DecrementRefCount();

        if (entry.RefCount <= 0 && disposeImmediately)
        {
            if (_servers.TryRemove(serverKey, out _))
            {
                await entry.Client.DisposeAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        var clients = _servers.Values.Select(e => e.Client).ToList();
        _servers.Clear();
        foreach (var c in clients)
        {
            await c.DisposeAsync();
        }
    }
}