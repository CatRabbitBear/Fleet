using Fleet.Runtime.Pipeline;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Releases plugin clients tracked by the PipelineContext.
/// </summary>
public class CleanupAgentsStep : IAgentPipelineStep
{
    public async Task ExecuteAsync(PipelineContext context)
    {
        foreach (var kvp in context.Agents)
        {
            foreach (var id in kvp.Value.PluginIds)
            {
                await context.PluginClientAdapter.ReleaseClientAsync(id);
            }
        }

        context.Agents.Clear();
    }
}
