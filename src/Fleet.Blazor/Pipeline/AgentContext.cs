﻿using Microsoft.SemanticKernel.Agents;

namespace Fleet.Blazor.Pipeline;

/// <summary>
/// Holds a created agent instance along with metadata needed for cleanup.
/// </summary>
public class AgentContext
{
    public ChatCompletionAgent Agent { get; }
    public IList<string> PluginIds { get; }

    public AgentContext(ChatCompletionAgent agent, IList<string>? pluginIds = null)
    {
        Agent = agent;
        PluginIds = pluginIds ?? new List<string>();
    }
}
