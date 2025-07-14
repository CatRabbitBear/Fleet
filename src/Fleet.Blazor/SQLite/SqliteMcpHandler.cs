using Dapper;
using Fleet.Blazor.PluginSystem.Dtos;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Fleet.Blazor.SQLite;

public class SqliteMcpHandler : SqliteHandlerBase
{
    // private readonly string _connectionString;

    protected override string? ConnectionString { get; set; }

    public SqliteMcpHandler(string connString)
    {
        ConnectionString = connString;
        EnsureTable().GetAwaiter().GetResult();
    }

    protected override async Task EnsureTable()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        const string sql = "CREATE TABLE IF NOT EXISTS McpServers (Id TEXT PRIMARY KEY, Manifest TEXT NOT NULL)";
        await connection.ExecuteAsync(sql);
    }

    public async Task<string> FetchAllMcpServers()
    {
        await using var connection = new SqliteConnection(ConnectionString);
        var manifests = await connection.QueryAsync<string>("SELECT Manifest FROM McpServers");
        var items = string.Join(",", manifests.Select(m => m));
        return $"{{\"servers\":[{items}]}}";
    }

    public async Task InsertMcpServerAsync(McpManifest manifest)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        const string sql = "INSERT OR REPLACE INTO McpServers (Id, Manifest) VALUES (@Id, @Manifest)";
        var json = JsonSerializer.Serialize(manifest);
        await connection.ExecuteAsync(sql, new { manifest.Id, Manifest = json });
    }
}