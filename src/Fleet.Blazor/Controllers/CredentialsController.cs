using Fleet.Blazor.Controllers.Dtos;
using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
using Fleet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;

[Route("api/credentials")]
[ApiController]
public sealed class CredentialsController : ControllerBase
{
    private readonly ICredentialHostService _credentialHostService;
    private readonly IPrivilegedActionExecutor _privilegedActionExecutor;
    private readonly ILocalSessionValidator _localSessionValidator;
    private readonly RequestIdentityContext _requestIdentity;

    public CredentialsController(
        ICredentialHostService credentialHostService,
        IPrivilegedActionExecutor privilegedActionExecutor,
        ILocalSessionValidator localSessionValidator,
        RequestIdentityContext requestIdentity)
    {
        _credentialHostService = credentialHostService;
        _privilegedActionExecutor = privilegedActionExecutor;
        _localSessionValidator = localSessionValidator;
        _requestIdentity = requestIdentity;
    }

    [HttpPost("set")]
    public async Task<ActionResult<CredentialCommandResult>> SetCredential([FromBody] SetCredentialRequest request, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        if (!_credentialHostService.IsAllowedTarget(request.Target))
        {
            return BadRequest(new { error = "Unknown or unsupported credential target." });
        }

        var action = new ActionDescriptor(ActionType.CredentialWrite, request.Target, RiskLevel.High, _requestIdentity.Current.RequestedBy);
        var authorization = await _privilegedActionExecutor.AuthorizeAsync(action, cancellationToken);
        if (!authorization.Allowed)
        {
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, authorization.Outcome, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Credential write denied by policy." });
        }

        var result = await _credentialHostService.SetCredentialAsync(request.Target, request.Value, _requestIdentity.Current.CorrelationId, cancellationToken);
        await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, result.Success ? "Success" : "HostFailure", cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<ActionResult<CredentialCommandResult>> DeleteCredential([FromBody] DeleteCredentialRequest request, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        if (!_credentialHostService.IsAllowedTarget(request.Target))
        {
            return BadRequest(new { error = "Unknown or unsupported credential target." });
        }

        var action = new ActionDescriptor(ActionType.CredentialDelete, request.Target, RiskLevel.High, _requestIdentity.Current.RequestedBy);
        var authorization = await _privilegedActionExecutor.AuthorizeAsync(action, cancellationToken);
        if (!authorization.Allowed)
        {
            await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, authorization.Outcome, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Credential delete denied by policy." });
        }

        var result = await _credentialHostService.DeleteCredentialAsync(request.Target, _requestIdentity.Current.CorrelationId, cancellationToken);
        await _privilegedActionExecutor.AuditAsync(action, authorization.Policy, result.Success ? "Success" : "HostFailure", cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("metadata/{target}")]
    public async Task<ActionResult<CredentialMetadata>> GetMetadata(string target, CancellationToken cancellationToken)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        if (!_credentialHostService.IsAllowedTarget(target))
        {
            return BadRequest(new { error = "Unknown or unsupported credential target." });
        }

        var action = new ActionDescriptor(ActionType.CredentialReadMetadata, target, RiskLevel.Low, _requestIdentity.Current.RequestedBy);
        var policy = new PolicyEvaluationResult(PolicyDecision.Allow, "Metadata reads are allowed for authenticated local session.");
        var metadata = await _credentialHostService.GetCredentialMetadataAsync(target, cancellationToken);
        await _privilegedActionExecutor.AuditAsync(action, policy, "Success", cancellationToken);
        return Ok(metadata);
    }
}
