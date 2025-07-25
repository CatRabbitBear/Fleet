﻿using Fleet.Blazor.Pipeline;
using Fleet.Blazor.Pipeline.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Fleet.Blazor.Agents.PipelineSteps;

/// <summary>
/// Runs the chat completion service using the built chat history.
/// </summary>
public class RunChatCompletionStep : IAgentPipelineStep
{
    public async Task ExecuteAsync(PipelineContext context)
    {
        if (context.ChatHistory == null)
        {
            return;
        }

        var chatService = context.Kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(context.ChatHistory);

        context.FinalResult = result.Content ?? result.ToString();
    }
}