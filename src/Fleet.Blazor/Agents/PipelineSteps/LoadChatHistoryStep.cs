using Fleet.Blazor.Utilities;
using Fleet.Runtime.Pipeline;

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
