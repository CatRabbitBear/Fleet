using Dapper;
using Fleet.Shared;
using Microsoft.Data.Sqlite;

namespace Fleet.Data;

public sealed class SqliteAuditRepository : IAuditRepository
{
    private readonly string _connectionString;

    public SqliteAuditRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS permission_audit (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                correlation_id TEXT NOT NULL,
                source TEXT NOT NULL,
                requested_by TEXT NOT NULL,
                action_type TEXT NOT NULL,
                resource TEXT NOT NULL,
                risk_level TEXT NOT NULL,
                policy_decision TEXT NOT NULL,
                final_outcome TEXT NOT NULL,
                rationale TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_permission_audit_correlation_id
            ON permission_audit(correlation_id);

            CREATE INDEX IF NOT EXISTS idx_permission_audit_resource_timestamp
            ON permission_audit(resource, timestamp_utc DESC);
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO permission_audit
            (timestamp_utc, correlation_id, source, requested_by, action_type, resource, risk_level, policy_decision, final_outcome, rationale)
            VALUES
            (@TimestampUtc, @CorrelationId, @Source, @RequestedBy, @ActionType, @Resource, @RiskLevel, @PolicyDecision, @FinalOutcome, @Rationale);
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            TimestampUtc = record.TimestampUtc.UtcDateTime,
            record.CorrelationId,
            Source = record.Source.ToString(),
            record.RequestedBy,
            ActionType = record.ActionType.ToString(),
            record.Resource,
            RiskLevel = record.RiskLevel.ToString(),
            PolicyDecision = record.PolicyDecision.ToString(),
            record.FinalOutcome,
            record.Rationale,
        }, cancellationToken: cancellationToken));
    }

    public async Task<DateTimeOffset?> GetLatestCredentialUpdateAsync(string target, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT timestamp_utc
            FROM permission_audit
            WHERE resource = @target
              AND action_type IN ('CredentialWrite', 'CredentialDelete')
              AND final_outcome = 'Success'
            ORDER BY timestamp_utc DESC
            LIMIT 1;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var value = await conn.QueryFirstOrDefaultAsync<DateTime?>(new CommandDefinition(sql, new { target }, cancellationToken: cancellationToken));
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    public async Task<IReadOnlyList<AuditRecordQueryResult>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                timestamp_utc AS TimestampUtc,
                correlation_id AS CorrelationId,
                source AS Source,
                requested_by AS RequestedBy,
                action_type AS ActionType,
                resource AS Resource,
                risk_level AS RiskLevel,
                policy_decision AS PolicyDecision,
                final_outcome AS FinalOutcome,
                rationale AS Rationale
            FROM permission_audit
            ORDER BY timestamp_utc DESC
            LIMIT @limit;
            """;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var results = await conn.QueryAsync<AuditRecordQueryResult>(
            new CommandDefinition(sql, new { limit }, cancellationToken: cancellationToken));

        return results.ToList();
    }
}
