namespace Fleet.Shared;

public enum RequestSourceType
{
    BlazorUiInteractive,
    BrowserExtension,
    InternalSystem,
    UnknownLocalCaller
}

public enum ActionType
{
    CredentialWrite,
    CredentialDelete,
    CredentialReadMetadata,
    ProcessSpawn,
    FileWrite,
    NetworkEgress,
    AuditRead
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum PolicyDecision
{
    Allow,
    Deny,
    RequireInteractiveConsent
}

public sealed record RequestIdentity(
    RequestSourceType Source,
    string RequestedBy,
    string CorrelationId);

public sealed record ActionDescriptor(
    ActionType ActionType,
    string Resource,
    RiskLevel RiskLevel,
    string RequestedBy);

public sealed record PolicyEvaluationResult(
    PolicyDecision Decision,
    string Rationale);

public sealed record AuditRecord(
    DateTimeOffset TimestampUtc,
    string CorrelationId,
    RequestSourceType Source,
    string RequestedBy,
    ActionType ActionType,
    string Resource,
    RiskLevel RiskLevel,
    PolicyDecision PolicyDecision,
    string FinalOutcome,
    string Rationale);

public sealed record AuditRecordQueryResult(
    DateTimeOffset TimestampUtc,
    string CorrelationId,
    string Source,
    string RequestedBy,
    string ActionType,
    string Resource,
    string RiskLevel,
    string PolicyDecision,
    string FinalOutcome,
    string Rationale);

public sealed record CredentialMetadata(
    string Target,
    bool Exists,
    DateTimeOffset? LastUpdatedUtc);

public sealed record CredentialCommandResult(
    bool Success,
    string CorrelationId,
    string Message,
    string? ErrorCode = null);

public interface ICredentialHostService
{
    Task<CredentialCommandResult> SetCredentialAsync(string target, string value, string correlationId, CancellationToken cancellationToken);
    Task<CredentialCommandResult> DeleteCredentialAsync(string target, string correlationId, CancellationToken cancellationToken);
    Task<CredentialMetadata> GetCredentialMetadataAsync(string target, CancellationToken cancellationToken);
    bool IsAllowedTarget(string target);
}

public interface IAuditRepository
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken);
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetLatestCredentialUpdateAsync(string target, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditRecordQueryResult>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}
