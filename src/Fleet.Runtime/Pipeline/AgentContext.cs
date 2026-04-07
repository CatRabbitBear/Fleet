namespace Fleet.Runtime.Pipeline;

/// <summary>
/// Runtime metadata tracked for a logical agent instance.
/// </summary>
public class AgentContext
{
    public IList<string> PluginIds { get; }

    public AgentContext(IList<string>? pluginIds = null)
    {
        PluginIds = pluginIds ?? new List<string>();
    }
}
