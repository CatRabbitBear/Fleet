namespace Fleet.Runtime.Security;

public enum RuntimeExecutionRisk
{
    Low,
    Medium,
    High
}

/// <summary>
/// Runtime-facing execution request used to invoke host-owned privileged operations.
/// </summary>
public sealed record RuntimeExecutionRequest(
    string Action,
    string Resource,
    RuntimeExecutionRisk Risk,
    string RequestedBy,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record RuntimeExecutionDecision(
    bool Allowed,
    string Outcome,
    string Rationale);

public interface IRuntimeExecutionGate
{
    Task<RuntimeExecutionDecision> AuthorizeAsync(RuntimeExecutionRequest request, CancellationToken cancellationToken = default);
    Task AuditAsync(RuntimeExecutionRequest request, RuntimeExecutionDecision decision, string finalOutcome, CancellationToken cancellationToken = default);
}
