using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Interfaces;
using Fleet.Blazor.Utilities;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Converts the request history into a ChatHistory instance.
/// </summary>
public class LoadChatHistoryStep : IAgentPipelineStep
{
    public Task ExecuteAsync(PipelineContext context)
    {
        context.ChatHistory = ChatHistoryBuilder.FromChatRequest(context.RequestHistory);
        return Task.CompletedTask;
    }
}
