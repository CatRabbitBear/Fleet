namespace Fleet.Runtime.Adapters;

/// <summary>
/// Host-owned filesystem adapter for runtime tool execution.
/// </summary>
public interface IFileSystemHostAdapter
{
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default);
}
