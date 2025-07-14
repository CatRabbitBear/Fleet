namespace Fleet.Blazor.SQLite;

public abstract class SqliteHandlerBase
{
    protected virtual string? ConnectionString { get; set; }

    protected abstract Task EnsureTable();
}

