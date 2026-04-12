namespace Fleet.Shared;

public enum ExtensionPermissionTier
{
    Low,
    Medium,
    High
}

public sealed record AgentDefinitionRecord(
    string AgentId,
    string Name,
    string Description,
    bool IsActive,
    string ActiveVersionId,
    ExtensionPermissionTier ExtensionTier,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AgentDefinitionVersionRecord(
    string VersionId,
    string AgentId,
    string PromptTemplate,
    string ModelPolicy,
    string AllowedToolsJson,
    string AllowedResourcesJson,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy);

public sealed record AgentRunRecord(
    string RunId,
    string AgentId,
    string AgentVersionId,
    string CorrelationId,
    string InitiatedBy,
    string Source,
    string PolicyDecision,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Error);

public sealed record AgentArtifactRecord(
    string ArtifactId,
    string RunId,
    string Type,
    string StoragePath,
    string Checksum,
    long ContentSize,
    DateTimeOffset CreatedAtUtc);

public sealed record AgentUpsertCommand(
    string Name,
    string Description,
    string PromptTemplate,
    string ModelPolicy,
    string[] AllowedTools,
    string[] AllowedResources,
    ExtensionPermissionTier ExtensionTier,
    string RequestedBy);

public interface IAgentGovernanceRepository
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken);
    Task<AgentDefinitionRecord> CreateAgentAsync(AgentUpsertCommand command, CancellationToken cancellationToken);
    Task<AgentDefinitionRecord?> GetAgentAsync(string agentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentDefinitionRecord>> ListAgentsAsync(CancellationToken cancellationToken);
    Task<AgentDefinitionVersionRecord?> GetActiveVersionAsync(string agentId, CancellationToken cancellationToken);
    Task<string> StartRunAsync(string agentId, string correlationId, string initiatedBy, string source, string policyDecision, CancellationToken cancellationToken);
    Task CompleteRunAsync(string runId, string status, string? error, CancellationToken cancellationToken);
    Task AddArtifactAsync(string runId, string type, string storagePath, string checksum, long contentSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(string? agentId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentArtifactRecord>> ListArtifactsByRunAsync(string runId, CancellationToken cancellationToken);
}
