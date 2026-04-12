using Fleet.Blazor.Controllers.Dtos;
using Fleet.Blazor.Security;
using Fleet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;

[Route("api/agents")]
[ApiController]
public class AgentsController : ControllerBase
{
    private readonly IAgentGovernanceRepository _repository;
    private readonly ILocalSessionValidator _localSessionValidator;
    private readonly RequestIdentityContext _requestIdentity;

    public AgentsController(
        IAgentGovernanceRepository repository,
        ILocalSessionValidator localSessionValidator,
        RequestIdentityContext requestIdentity)
    {
        _repository = repository;
        _localSessionValidator = localSessionValidator;
        _requestIdentity = requestIdentity;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentDefinitionRecord>>> ListAsync(CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        return Ok(await _repository.ListAgentsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AgentDefinitionRecord>> CreateAsync([FromBody] CreateAgentRequest request, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.PromptTemplate))
        {
            return BadRequest(new { error = "Name and prompt template are required." });
        }

        var record = await _repository.CreateAgentAsync(new AgentUpsertCommand(
            request.Name.Trim(),
            request.Description?.Trim() ?? string.Empty,
            request.PromptTemplate,
            request.ModelPolicy ?? "default",
            request.AllowedTools ?? [],
            request.AllowedResources ?? [],
            request.ExtensionTier,
            _requestIdentity.Current.RequestedBy), cancellationToken);

        return Ok(record);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<AgentRunRecord>>> ListRunsAsync([FromQuery] string? agentId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        var normalizedLimit = Math.Clamp(limit, 1, 200);
        return Ok(await _repository.ListRunsAsync(agentId, normalizedLimit, cancellationToken));
    }

    [HttpGet("runs/{runId}/artifacts")]
    public async Task<ActionResult<IReadOnlyList<AgentArtifactRecord>>> ListArtifactsAsync(string runId, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        return Ok(await _repository.ListArtifactsByRunAsync(runId, cancellationToken));
    }
}
