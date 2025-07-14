using Fleet.Blazor.Pipeline.Interfaces;

namespace Fleet.Blazor.Pipeline;

/// <summary>
/// Simple builder used to construct an AgentPipeline.
/// </summary>
public class AgentPipelineBuilder : IAgentPipelineBuilder
{
    private readonly IList<IAgentPipelineStep> _steps = new List<IAgentPipelineStep>();

    public IAgentPipelineBuilder Use(IAgentPipelineStep step)
    {
        _steps.Add(step);
        return this;
    }

    public IAgentPipeline Build()
    {
        return new AgentPipeline(_steps.ToList());
    }
}
