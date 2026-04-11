using System.Diagnostics;
using Fleet.Runtime.Adapters;
using Fleet.Runtime.Security;

namespace Fleet.Tray.Utils;

/// <summary>
/// Tray-hosted privileged adapter implementation for runtime filesystem/process actions.
/// This keeps OS-sensitive execution in Fleet.Tray while still flowing through policy/audit hooks.
/// </summary>
public sealed class TrayRuntimeHostAdapters : IFileSystemHostAdapter, IProcessHostAdapter
{
    private readonly IRuntimeExecutionGate _executionGate;
    private readonly string _fleetRoot;

    public TrayRuntimeHostAdapters(IRuntimeExecutionGate executionGate)
    {
        _executionGate = executionGate;
        _fleetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fleet");
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeAndValidatePath(path);
        var request = new RuntimeExecutionRequest("file:read", normalizedPath, RuntimeExecutionRisk.Medium, "tray-host");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem read denied for '{normalizedPath}'.");
        }

        var content = await File.ReadAllTextAsync(normalizedPath, cancellationToken);
        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
        return content;
    }

    public async Task WriteTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeAndValidatePath(path);
        var request = new RuntimeExecutionRequest("file:write", normalizedPath, RuntimeExecutionRisk.High, "tray-host");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem write denied for '{normalizedPath}'.");
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(normalizedPath, content, cancellationToken);
        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizeAndValidatePath(path);
        var request = new RuntimeExecutionRequest("file:list", normalizedPath, RuntimeExecutionRisk.Medium, "tray-host");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Filesystem list denied for '{normalizedPath}'.");
        }

        var entries = Directory.Exists(normalizedPath)
            ? Directory.EnumerateFileSystemEntries(normalizedPath).ToList()
            : new List<string>();

        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
        return entries;
    }

    public async Task<int> StartProcessAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var request = new RuntimeExecutionRequest("process:spawn", fileName, RuntimeExecutionRisk.High, "tray-host");
        var decision = await _executionGate.AuthorizeAsync(request, cancellationToken);
        if (!decision.Allowed)
        {
            await _executionGate.AuditAsync(request, decision, "PolicyDenied", cancellationToken);
            throw new UnauthorizedAccessException($"Process execution denied for '{fileName}'.");
        }

        var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");

        await process.WaitForExitAsync(cancellationToken);
        await _executionGate.AuditAsync(request, decision, "Success", cancellationToken);
        return process.ExitCode;
    }

    private string NormalizeAndValidatePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(_fleetRoot);

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Path '{fullPath}' is outside the Fleet host root '{fullRoot}'.");
        }

        return fullPath;
    }
}
