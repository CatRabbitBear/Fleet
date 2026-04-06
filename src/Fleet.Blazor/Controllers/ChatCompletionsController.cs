using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
using Fleet.Blazor.Utilities;
using Fleet.Runtime.Agents;
using Fleet.Runtime.Contracts;
using Fleet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;

[Route("api/chat-completions")]
[ApiController]
public class ChatCompletionsController : ControllerBase
{
    private readonly ILogger<ChatCompletionsController> _logger;
    private readonly IChatCompletionsRunner _chatCompletionsRunner;
    private readonly IPrivilegedActionExecutor _privilegedActionExecutor;
    private readonly ILocalSessionValidator _localSessionValidator;
    private readonly RequestIdentityContext _requestIdentity;

    public ChatCompletionsController(
        ILogger<ChatCompletionsController> logger,
        IChatCompletionsRunner chatCompletionsRunner,
        IPrivilegedActionExecutor privilegedActionExecutor,
        ILocalSessionValidator localSessionValidator,
        RequestIdentityContext requestIdentity)
    {
        _logger = logger;
        _chatCompletionsRunner = chatCompletionsRunner;
        _privilegedActionExecutor = privilegedActionExecutor;
        _localSessionValidator = localSessionValidator;
        _requestIdentity = requestIdentity;
    }

    [HttpPost("run-task")]
    public async Task<ActionResult<AgentResponse>> RunTask([FromBody] AgentRequest req, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        var action = new ActionDescriptor(ActionType.NetworkEgress, "chat-completions:run-task", RiskLevel.High, _requestIdentity.Current.RequestedBy);
        var authorization = await _privilegedActionExecutor.AuthorizeAsync(action, cancellationToken);
        if (!authorization.Allowed)
        {
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, authorization.Outcome, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Task execution denied by policy." });
        }

        try
        {
            _logger.LogInformation("RunTask invoked. History count: {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask invoked. HistoryCount={req.History.Count}");

            var result = await _chatCompletionsRunner.RunTaskAsync(req.History);
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, "Success", cancellationToken);

            _logger.LogInformation("Task completed successfully by chat completions runner. Items returned : {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask completed successfully. HistoryCount={req.History.Count}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, "ExecutionFailure", cancellationToken);
            _logger.LogError(ex, "Error running task in chat completions runner.");
            ChatDiagnostics.Error("API RunTask failed.", ex);
            return BadRequest(new { error = ex.Message });
        }
    }
}
