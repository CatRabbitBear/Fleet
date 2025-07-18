using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Interfaces;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Disposes any agents stored in the PipelineContext and releases their plugin clients.
/// </summary>
public class CleanupAgentsStep : IAgentPipelineStep
{
    public async Task ExecuteAsync(PipelineContext context)
    {
        foreach (var kvp in context.Agents)
        {
            // Release any plugin clients acquired for this agent
            foreach (var id in kvp.Value.PluginIds)
            {
                await context.PluginManager.ReleaseClientAsync(id);
            }
        }
        context.Agents.Clear();
    }
}
