using System.Diagnostics;
using Fleet.Runtime.Adapters;
using Fleet.Runtime.Security;

namespace Fleet.Blazor.Adapters;

/// <summary>
/// Baseline host process adapter for standalone Blazor execution.
/// Tray can override this registration with stricter host-bound controls.
/// </summary>
public sealed class LocalProcessHostAdapter : IProcessHostAdapter
{
    private readonly IRuntimeExecutionGate _executionGate;

    public LocalProcessHostAdapter(IRuntimeExecutionGate executionGate)
    {
        _executionGate = executionGate;
    }

    public async Task<int> StartProcessAsync(string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var request = new RuntimeExecutionRequest("process:spawn", fileName, RuntimeExecutionRisk.High, "runtime-agent");
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
}
