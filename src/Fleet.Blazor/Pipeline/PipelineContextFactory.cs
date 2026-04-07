using Fleet.Runtime.Pipeline;
using Fleet.Runtime.Contracts;
using Microsoft.SemanticKernel;
using Fleet.Runtime.Adapters;

namespace Fleet.Blazor.Pipeline;

/// <summary>
/// Factory for creating <see cref="PipelineContext"/> instances with a cloned kernel.
/// </summary>
public class PipelineContextFactory : IPipelineContextFactory
{
    private readonly Kernel _kernel;
    private readonly IPluginClientAdapter _pluginClientAdapter;

    public PipelineContextFactory(Kernel kernel, IPluginClientAdapter pluginClientAdapter)
    {
        _kernel = kernel;
        _pluginClientAdapter = pluginClientAdapter;
    }

    /// <summary>
    /// Create a new context for the given request history. The kernel is cloned
    /// so that plugins can be added without affecting other contexts.
    /// </summary>
    public PipelineContext Create(List<AgentRequestItem> history)
    {
        var clone = _kernel.Clone();
        return new PipelineContext(history, clone, _pluginClientAdapter);
    }
}
