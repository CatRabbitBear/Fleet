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
    private readonly IAgentGovernanceRepository _agentGovernanceRepository;

    public ChatCompletionsController(
        ILogger<ChatCompletionsController> logger,
        IChatCompletionsRunner chatCompletionsRunner,
        IPrivilegedActionExecutor privilegedActionExecutor,
        ILocalSessionValidator localSessionValidator,
        RequestIdentityContext requestIdentity,
        IAgentGovernanceRepository agentGovernanceRepository)
    {
        _logger = logger;
        _chatCompletionsRunner = chatCompletionsRunner;
        _privilegedActionExecutor = privilegedActionExecutor;
        _localSessionValidator = localSessionValidator;
        _requestIdentity = requestIdentity;
        _agentGovernanceRepository = agentGovernanceRepository;
    }

    [HttpPost("run-task")]
    public async Task<ActionResult<AgentResponse>> RunTask([FromBody] AgentRequest req, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(req.AgentId))
        {
            return BadRequest(new { error = "AgentId is required for phase 3 execution lineage." });
        }

        var configuredAgent = await _agentGovernanceRepository.GetAgentAsync(req.AgentId, cancellationToken);
        if (configuredAgent is null || !configuredAgent.IsActive)
        {
            return BadRequest(new { error = "Agent is missing or inactive." });
        }

        if (_requestIdentity.Current.Source == RequestSourceType.BrowserExtension && configuredAgent.ExtensionTier == ExtensionPermissionTier.High)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "High-tier extension agent requires interactive trusted flow.",
                correlationId = _requestIdentity.Current.CorrelationId
            });
        }

        var action = new ActionDescriptor(ActionType.NetworkEgress, $"chat-completions:run-task:{req.AgentId}", RiskLevel.High, _requestIdentity.Current.RequestedBy);
        var authorization = await _privilegedActionExecutor.AuthorizeAsync(action, cancellationToken);
        var runId = await _agentGovernanceRepository.StartRunAsync(req.AgentId, _requestIdentity.Current.CorrelationId, _requestIdentity.Current.RequestedBy, _requestIdentity.Current.Source.ToString(), authorization.Policy.Decision.ToString(), cancellationToken);

        if (!authorization.Allowed)
        {
            await _agentGovernanceRepository.CompleteRunAsync(runId, "Denied", "Denied by policy", cancellationToken);
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, authorization.Outcome, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Task execution denied by policy.", runId, correlationId = _requestIdentity.Current.CorrelationId });
        }

        try
        {
            _logger.LogInformation("RunTask invoked. History count: {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask invoked. HistoryCount={req.History.Count}");

            var result = await _chatCompletionsRunner.RunTaskAsync(req.History);
            result.RunId = runId;

            await _agentGovernanceRepository.CompleteRunAsync(runId, "Success", null, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.FilePath))
            {
                await _agentGovernanceRepository.AddArtifactAsync(runId, "output-file", result.FilePath, result.Result, result.Result.Length, cancellationToken);
            }

            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, "Success", cancellationToken);

            _logger.LogInformation("Task completed successfully by chat completions runner. Items returned : {Count}", req.History.Count);
            ChatDiagnostics.Info($"API RunTask completed successfully. HistoryCount={req.History.Count}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _agentGovernanceRepository.CompleteRunAsync(runId, "ExecutionFailure", ex.Message, cancellationToken);
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, "ExecutionFailure", cancellationToken);
            _logger.LogError(ex, "Error running task in chat completions runner.");
            ChatDiagnostics.Error("API RunTask failed.", ex);
            return BadRequest(new { error = ex.Message, runId, correlationId = _requestIdentity.Current.CorrelationId });
        }
    }
}
