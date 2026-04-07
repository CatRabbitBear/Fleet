namespace Fleet.Runtime.Adapters;

/// <summary>
/// Host-owned adapter for persisting agent output.
/// </summary>
public interface IAgentOutputStore
{
    Task SaveOutputAsync(string content, CancellationToken cancellationToken = default);
}
