using Fleet.Blazor.Agents.PipelineSteps;
using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Dtos;
using Fleet.Blazor.Pipeline.Interfaces;
using Fleet.Blazor.SQLite;
using Microsoft.SemanticKernel.Agents;

namespace Fleet.Blazor.Agents;

public class ChatCompletionsRunner
{
    private readonly ILogger<ChatCompletionsRunner> _logger;
    private readonly IPipelineContextFactory _contextFactory;
    private readonly SqliteAgentOutputHandler _outputManager;

    public ChatCompletionsRunner(
        ILogger<ChatCompletionsRunner> logger,
        IPipelineContextFactory contextFactory,
        SqliteAgentOutputHandler outputManager)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _outputManager = outputManager;
    }

    public async Task<AgentResponse> RunTaskAsync(List<AgentRequestItem> history)
    {
        var pipeline = new AgentPipelineBuilder()
            .Use(new LoadChatHistoryStep())
            .Use(new RunChatCompletionStep())
            .Use(new SaveOutputStep(_outputManager))
            .Build();

        var context = _contextFactory.Create(history);
        await pipeline.RunAsync(context);

        return new AgentResponse
        {
            Result = context.FinalResult ?? string.Empty,
            FilePath = context.FilePath
        };
    }
}
