using Dapper;
using Microsoft.Data.Sqlite;

namespace Fleet.Blazor.SQLite;

public class SqliteAgentOutputHandler : SqliteHandlerBase
{
    protected override string? ConnectionString { get; set; }

    public SqliteAgentOutputHandler(string connString)
    {
        ConnectionString = connString;
        // Make sure the table is there before we do anything
        EnsureTable().GetAwaiter().GetResult();
    }

    protected override async Task EnsureTable()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        const string sql = @"
                CREATE TABLE IF NOT EXISTS AgentOutputs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Content TEXT    NOT NULL,
                    CreatedAt TEXT  NOT NULL
                );";
        await connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Saves a piece of agent-generated content to the DB.
    /// </summary>
    /// <param name="content">The text you want to persist.</param>
    public async Task SaveAgentOutputAsync(string content)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        const string insertSql = @"
                INSERT INTO AgentOutputs (Content, CreatedAt)
                VALUES (@Content, @CreatedAt);";

        await connection.ExecuteAsync(insertSql, new
        {
            Content = content,
            CreatedAt = DateTime.UtcNow.ToString("o")
        });
    }
}
