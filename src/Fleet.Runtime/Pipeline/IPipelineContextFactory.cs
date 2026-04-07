using Fleet.Runtime.Contracts;

namespace Fleet.Runtime.Pipeline;

/// <summary>
/// Creates <see cref="PipelineContext"/> instances for agent pipelines.
/// </summary>
public interface IPipelineContextFactory
{
    PipelineContext Create(List<AgentRequestItem> history);
}
