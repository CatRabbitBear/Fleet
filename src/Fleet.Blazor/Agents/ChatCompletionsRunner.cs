using Fleet.Blazor.Agents.PipelineSteps;
using Fleet.Blazor.Pipeline;
using Fleet.Runtime.Adapters;
using Fleet.Runtime.Agents;
using Fleet.Runtime.Contracts;
using Fleet.Runtime.Pipeline;

namespace Fleet.Blazor.Agents;

public class ChatCompletionsRunner : IChatCompletionsRunner
{
    private readonly ILogger<ChatCompletionsRunner> _logger;
    private readonly IPipelineContextFactory _contextFactory;
    private readonly IAgentOutputStore _outputStore;

    public ChatCompletionsRunner(
        ILogger<ChatCompletionsRunner> logger,
        IPipelineContextFactory contextFactory,
        IAgentOutputStore outputStore)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _outputStore = outputStore;
    }

    public async Task<AgentResponse> RunTaskAsync(List<AgentRequestItem> history)
    {
        var pipeline = new AgentPipelineBuilder()
            .Use(new LoadChatHistoryStep())
            .Use(new RunChatCompletionStep())
            .Use(new SaveOutputStep(_outputStore))
            .Build();

        _logger.LogDebug("Starting chat completions pipeline with {HistoryCount} history items", history.Count);
        var context = _contextFactory.Create(history);
        await pipeline.RunAsync(context);

        return new AgentResponse
        {
            Result = context.FinalResult ?? string.Empty,
            FilePath = context.FilePath
        };
    }
}
