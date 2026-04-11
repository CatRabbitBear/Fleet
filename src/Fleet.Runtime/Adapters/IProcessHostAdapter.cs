namespace Fleet.Runtime.Adapters;

/// <summary>
/// Host-owned process adapter for runtime tool execution.
/// </summary>
public interface IProcessHostAdapter
{
    Task<int> StartProcessAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}
