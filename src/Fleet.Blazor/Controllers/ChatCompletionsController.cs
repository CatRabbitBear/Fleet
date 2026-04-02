using Fleet.Blazor.Agents;
using Fleet.Blazor.Pipeline.Dtos;
using Fleet.Blazor.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;
[Route("api/chat-completions")]
[ApiController]
public class ChatCompletionsController : ControllerBase
{
    private readonly ILogger<ChatCompletionsController> _logger;
    private readonly ChatCompletionsRunner _chatCompletionsRunner;
    public ChatCompletionsController(ILogger<ChatCompletionsController> logger, ChatCompletionsRunner chatCompletionsRunner)
    {
        _logger = logger;
        _chatCompletionsRunner = chatCompletionsRunner;
    }

    [HttpPost("run-task")]
    public async Task<ActionResult<AgentResponse>> RunTask([FromBody] AgentRequest req)
    {
        try
        {
            _logger.LogInformation("RunTask invoked. History count: {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask invoked. HistoryCount={req.History.Count}");

            var result = await _chatCompletionsRunner.RunTaskAsync(req.History);

            _logger.LogInformation("Task completed successfully by chat completions runner. Items returned : {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask completed successfully. HistoryCount={req.History.Count}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running task in chat completions runner.");
            ChatDiagnostics.Error("API RunTask failed.", ex);
            return BadRequest(new { error = ex.Message });
        }
    }
}
