using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;
[Route("api/chat-completions")]
[ApiController]
public class ChatCompletionsController : ControllerBase
{
    private readonly ILogger<ChatCompletionsController> _logger;
    //private readonly IChatCompletionsRunner _chatCompletionsRunner;
    public ChatCompletionsController(ILogger<ChatCompletionsController> logger)
    {
        _logger = logger;
        //_chatCompletionsRunner = chatCompletionsRunner;
    }

    [HttpPost("run-task")]
    public async Task<ActionResult<AgentResponse>> RunTask([FromBody] AgentRequest req)
    {
        try
        {
            //var result = await _chatCompletionsRunner.RunTaskAsync(req.History);
            var result = new AgentResponse
            {
                Result = "This is a mock response for the task.",
                FilePath = null // or set to a valid file path if needed
            };
            _logger.LogInformation("Task completed successfully by chat completions runner. Items returned : {Count}", req.History.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running task in chat completions runner.");
            return BadRequest(new { error = ex.Message });
        }
    }
}
