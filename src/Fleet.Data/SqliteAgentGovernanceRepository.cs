using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Fleet.Shared;
using Microsoft.Data.Sqlite;

namespace Fleet.Data;

public sealed class SqliteAgentGovernanceRepository : IAgentGovernanceRepository
{
    private readonly string _connectionString;

    public SqliteAgentGovernanceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS agent_definitions (
                agent_id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT NOT NULL,
                is_active INTEGER NOT NULL,
                active_version_id TEXT NOT NULL,
                extension_tier TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS agent_definition_versions (
                version_id TEXT PRIMARY KEY,
                agent_id TEXT NOT NULL,
                prompt_template TEXT NOT NULL,
                model_policy TEXT NOT NULL,
                allowed_tools_json TEXT NOT NULL,
                allowed_resources_json TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                created_by TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS agent_runs (
                run_id TEXT PRIMARY KEY,
                agent_id TEXT NOT NULL,
                agent_version_id TEXT NOT NULL,
                correlation_id TEXT NOT NULL,
                initiated_by TEXT NOT NULL,
                source TEXT NOT NULL,
                policy_decision TEXT NOT NULL,
                status TEXT NOT NULL,
                started_at_utc TEXT NOT NULL,
                completed_at_utc TEXT NULL,
                error TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS agent_artifacts (
                artifact_id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                type TEXT NOT NULL,
                storage_path TEXT NOT NULL,
                checksum TEXT NOT NULL,
                content_size INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_agent_runs_started_at
            ON agent_runs(started_at_utc DESC);
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<AgentDefinitionRecord> CreateAgentAsync(AgentUpsertCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var agentId = Guid.NewGuid().ToString("N");
        var versionId = Guid.NewGuid().ToString("N");
        var toolsJson = JsonSerializer.Serialize(command.AllowedTools);
        var resourcesJson = JsonSerializer.Serialize(command.AllowedResources);

        const string insertAgent = """
            INSERT INTO agent_definitions (agent_id, name, description, is_active, active_version_id, extension_tier, created_at_utc, updated_at_utc)
            VALUES (@AgentId, @Name, @Description, 1, @VersionId, @ExtensionTier, @NowUtc, @NowUtc);
            """;

        const string insertVersion = """
            INSERT INTO agent_definition_versions (version_id, agent_id, prompt_template, model_policy, allowed_tools_json, allowed_resources_json, created_at_utc, created_by)
            VALUES (@VersionId, @AgentId, @PromptTemplate, @ModelPolicy, @AllowedToolsJson, @AllowedResourcesJson, @NowUtc, @CreatedBy);
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(insertAgent, new
        {
            AgentId = agentId,
            command.Name,
            command.Description,
            VersionId = versionId,
            ExtensionTier = command.ExtensionTier.ToString(),
            NowUtc = now.UtcDateTime
        }, cancellationToken: cancellationToken));

        await conn.ExecuteAsync(new CommandDefinition(insertVersion, new
        {
            VersionId = versionId,
            AgentId = agentId,
            command.PromptTemplate,
            command.ModelPolicy,
            AllowedToolsJson = toolsJson,
            AllowedResourcesJson = resourcesJson,
            NowUtc = now.UtcDateTime,
            CreatedBy = command.RequestedBy
        }, cancellationToken: cancellationToken));

        return new AgentDefinitionRecord(agentId, command.Name, command.Description, true, versionId, command.ExtensionTier, now, now);
    }

    public async Task<AgentDefinitionRecord?> GetAgentAsync(string agentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                agent_id AS AgentId,
                name AS Name,
                description AS Description,
                is_active AS IsActive,
                active_version_id AS ActiveVersionId,
                extension_tier AS ExtensionTier,
                created_at_utc AS CreatedAtUtc,
                updated_at_utc AS UpdatedAtUtc
            FROM agent_definitions
            WHERE agent_id = @agentId;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var row = await conn.QueryFirstOrDefaultAsync<AgentDefinitionQueryRow>(new CommandDefinition(sql, new { agentId }, cancellationToken: cancellationToken));
        return row is null ? null : ToDefinition(row);
    }

    public async Task<IReadOnlyList<AgentDefinitionRecord>> ListAgentsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                agent_id AS AgentId,
                name AS Name,
                description AS Description,
                is_active AS IsActive,
                active_version_id AS ActiveVersionId,
                extension_tier AS ExtensionTier,
                created_at_utc AS CreatedAtUtc,
                updated_at_utc AS UpdatedAtUtc
            FROM agent_definitions
            ORDER BY updated_at_utc DESC;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<AgentDefinitionQueryRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(ToDefinition).ToList();
    }

    public async Task<AgentDefinitionVersionRecord?> GetActiveVersionAsync(string agentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                v.version_id AS VersionId,
                v.agent_id AS AgentId,
                v.prompt_template AS PromptTemplate,
                v.model_policy AS ModelPolicy,
                v.allowed_tools_json AS AllowedToolsJson,
                v.allowed_resources_json AS AllowedResourcesJson,
                v.created_at_utc AS CreatedAtUtc,
                v.created_by AS CreatedBy
            FROM agent_definition_versions v
            INNER JOIN agent_definitions d ON d.active_version_id = v.version_id
            WHERE d.agent_id = @agentId;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var row = await conn.QueryFirstOrDefaultAsync<AgentVersionQueryRow>(new CommandDefinition(sql, new { agentId }, cancellationToken: cancellationToken));
        return row is null ? null : new AgentDefinitionVersionRecord(row.VersionId, row.AgentId, row.PromptTemplate, row.ModelPolicy, row.AllowedToolsJson, row.AllowedResourcesJson, new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Utc)), row.CreatedBy);
    }

    public async Task<string> StartRunAsync(string agentId, string correlationId, string initiatedBy, string source, string policyDecision, CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var active = await GetAgentAsync(agentId, cancellationToken) ?? throw new InvalidOperationException($"Agent {agentId} not found.");

        const string sql = """
            INSERT INTO agent_runs (run_id, agent_id, agent_version_id, correlation_id, initiated_by, source, policy_decision, status, started_at_utc)
            VALUES (@RunId, @AgentId, @AgentVersionId, @CorrelationId, @InitiatedBy, @Source, @PolicyDecision, 'Running', @StartedAtUtc);
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            RunId = runId,
            AgentId = agentId,
            AgentVersionId = active.ActiveVersionId,
            CorrelationId = correlationId,
            InitiatedBy = initiatedBy,
            Source = source,
            PolicyDecision = policyDecision,
            StartedAtUtc = now.UtcDateTime
        }, cancellationToken: cancellationToken));

        return runId;
    }

    public async Task CompleteRunAsync(string runId, string status, string? error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE agent_runs
            SET status = @status,
                error = @error,
                completed_at_utc = @completedAtUtc
            WHERE run_id = @runId;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            runId,
            status,
            error,
            completedAtUtc = DateTimeOffset.UtcNow.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task AddArtifactAsync(string runId, string type, string storagePath, string checksum, long contentSize, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO agent_artifacts (artifact_id, run_id, type, storage_path, checksum, content_size, created_at_utc)
            VALUES (@ArtifactId, @RunId, @Type, @StoragePath, @Checksum, @ContentSize, @CreatedAtUtc);
            """;

        var payload = string.IsNullOrWhiteSpace(checksum) ? storagePath : checksum;
        var computedChecksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ArtifactId = Guid.NewGuid().ToString("N"),
            RunId = runId,
            Type = type,
            StoragePath = storagePath,
            Checksum = computedChecksum,
            ContentSize = contentSize,
            CreatedAtUtc = DateTimeOffset.UtcNow.UtcDateTime
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(string? agentId, int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                run_id AS RunId,
                agent_id AS AgentId,
                agent_version_id AS AgentVersionId,
                correlation_id AS CorrelationId,
                initiated_by AS InitiatedBy,
                source AS Source,
                policy_decision AS PolicyDecision,
                status AS Status,
                started_at_utc AS StartedAtUtc,
                completed_at_utc AS CompletedAtUtc,
                error AS Error
            FROM agent_runs
            WHERE @agentId IS NULL OR agent_id = @agentId
            ORDER BY started_at_utc DESC
            LIMIT @limit;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<AgentRunQueryRow>(new CommandDefinition(sql, new { agentId, limit }, cancellationToken: cancellationToken));

        return rows.Select(row => new AgentRunRecord(
            row.RunId,
            row.AgentId,
            row.AgentVersionId,
            row.CorrelationId,
            row.InitiatedBy,
            row.Source,
            row.PolicyDecision,
            row.Status,
            new DateTimeOffset(DateTime.SpecifyKind(row.StartedAtUtc, DateTimeKind.Utc)),
            row.CompletedAtUtc is null ? null : new DateTimeOffset(DateTime.SpecifyKind(row.CompletedAtUtc.Value, DateTimeKind.Utc)),
            row.Error)).ToList();
    }

    public async Task<IReadOnlyList<AgentArtifactRecord>> ListArtifactsByRunAsync(string runId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                artifact_id AS ArtifactId,
                run_id AS RunId,
                type AS Type,
                storage_path AS StoragePath,
                checksum AS Checksum,
                content_size AS ContentSize,
                created_at_utc AS CreatedAtUtc
            FROM agent_artifacts
            WHERE run_id = @runId
            ORDER BY created_at_utc DESC;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var rows = await conn.QueryAsync<AgentArtifactQueryRow>(new CommandDefinition(sql, new { runId }, cancellationToken: cancellationToken));

        return rows.Select(row => new AgentArtifactRecord(
            row.ArtifactId,
            row.RunId,
            row.Type,
            row.StoragePath,
            row.Checksum,
            row.ContentSize,
            new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Utc)))).ToList();
    }

    private static AgentDefinitionRecord ToDefinition(AgentDefinitionQueryRow row)
        => new(
            row.AgentId,
            row.Name,
            row.Description,
            row.IsActive,
            row.ActiveVersionId,
            Enum.Parse<ExtensionPermissionTier>(row.ExtensionTier, true),
            new DateTimeOffset(DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(row.UpdatedAtUtc, DateTimeKind.Utc)));

    private sealed class AgentDefinitionQueryRow
    {
        public string AgentId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string ActiveVersionId { get; init; } = string.Empty;
        public string ExtensionTier { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }

    private sealed class AgentVersionQueryRow
    {
        public string VersionId { get; init; } = string.Empty;
        public string AgentId { get; init; } = string.Empty;
        public string PromptTemplate { get; init; } = string.Empty;
        public string ModelPolicy { get; init; } = string.Empty;
        public string AllowedToolsJson { get; init; } = string.Empty;
        public string AllowedResourcesJson { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
    }

    private sealed class AgentRunQueryRow
    {
        public string RunId { get; init; } = string.Empty;
        public string AgentId { get; init; } = string.Empty;
        public string AgentVersionId { get; init; } = string.Empty;
        public string CorrelationId { get; init; } = string.Empty;
        public string InitiatedBy { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string PolicyDecision { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime StartedAtUtc { get; init; }
        public DateTime? CompletedAtUtc { get; init; }
        public string? Error { get; init; }
    }

    private sealed class AgentArtifactQueryRow
    {
        public string ArtifactId { get; init; } = string.Empty;
        public string RunId { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string StoragePath { get; init; } = string.Empty;
        public string Checksum { get; init; } = string.Empty;
        public long ContentSize { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
