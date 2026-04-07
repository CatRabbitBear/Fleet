using Fleet.Runtime.Adapters;
using Fleet.Runtime.Pipeline;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Persists the final result using a host-owned output store.
/// </summary>
public class SaveOutputStep : IAgentPipelineStep
{
    private readonly IAgentOutputStore _outputStore;

    public SaveOutputStep(IAgentOutputStore outputStore)
    {
        _outputStore = outputStore;
    }

    public async Task ExecuteAsync(PipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FinalResult))
        {
            return;
        }

        try
        {
            await _outputStore.SaveOutputAsync(context.FinalResult);
        }
        catch (Exception)
        {
            // intentionally swallow to avoid failing request due to storage issues
        }
    }
}
