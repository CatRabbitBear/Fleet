using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Interfaces;
using Fleet.Blazor.SQLite;
using Microsoft.SemanticKernel.Agents;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Persists the final result using an IAgentOutput implementation.
/// </summary>
public class SaveOutputStep : IAgentPipelineStep
{
    private readonly SqliteAgentOutputHandler _output;

    public SaveOutputStep(SqliteAgentOutputHandler output)
    {
        _output = output;
    }

    public async Task ExecuteAsync(PipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FinalResult))
        {
            return;
        }

        try
        {
            await _output.SaveAgentOutputAsync(context.FinalResult);
        }
        catch (Exception ex)
        {
            // Log
        }
    }
}
