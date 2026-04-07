using Fleet.Runtime.Adapters;
using Fleet.Runtime.Contracts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Fleet.Runtime.Pipeline;

/// <summary>
/// Carries request data and runtime state between pipeline steps.
/// </summary>
public class PipelineContext
{
    public List<AgentRequestItem> RequestHistory { get; }
    public Kernel Kernel { get; set; }
    public IPluginClientAdapter PluginClientAdapter { get; }
    public IList<string> SelectedPlugins { get; set; } = new List<string>();
    public ChatHistory? ChatHistory { get; set; }
    public string? FinalResult { get; set; }
    public string? FilePath { get; set; }
    public Dictionary<string, AgentContext> Agents { get; } = new();

    public PipelineContext(
        List<AgentRequestItem> history,
        Kernel kernel,
        IPluginClientAdapter pluginClientAdapter)
    {
        RequestHistory = history;
        Kernel = kernel;
        PluginClientAdapter = pluginClientAdapter;
    }
}
