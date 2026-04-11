using Fleet.Runtime.Adapters;
using Fleet.Runtime.Security;

namespace Fleet.Blazor.Adapters;

/// <summary>
/// Baseline host filesystem adapter for standalone Blazor execution.
/// Tray can override this registration with stricter host-bound controls.
/// </summary>
public sealed class LocalFileSystemHostAdapter : IFileSystemHostAdapter
{
    private readonly IRuntimeExecutionGate _executionGate;

    public LocalFileSystemHostAdapter(IRuntimeExecutionGate executionGate)
    {
        _executionGate = executionGate;
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var request = new RuntimeExecutionRequest("file:read", path, RuntimeExecutionRisk.Medium, "runtime-agent");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem read denied for '{path}'.");
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
        return content;
    }

    public async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var request = new RuntimeExecutionRequest("file:write", path, RuntimeExecutionRisk.High, "runtime-agent");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem write denied for '{path}'.");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken);
        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var request = new RuntimeExecutionRequest("file:list", path, RuntimeExecutionRisk.Medium, "runtime-agent");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem list denied for '{path}'.");
        }

        var entries = Directory.Exists(path)
            ? Directory.EnumerateFileSystemEntries(path).ToList()
            : new List<string>();

        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
        return entries;
    }
}
