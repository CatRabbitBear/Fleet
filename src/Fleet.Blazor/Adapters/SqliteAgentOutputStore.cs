using Fleet.Blazor.SQLite;
using Fleet.Runtime.Adapters;

namespace Fleet.Blazor.Adapters;

/// <summary>
/// Blazor host adapter for persisting agent outputs to sqlite.
/// </summary>
public class SqliteAgentOutputStore : IAgentOutputStore
{
    private readonly SqliteAgentOutputHandler _handler;

    public SqliteAgentOutputStore(SqliteAgentOutputHandler handler)
    {
        _handler = handler;
    }

    public Task SaveOutputAsync(string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _handler.SaveAgentOutputAsync(content);
    }
}
