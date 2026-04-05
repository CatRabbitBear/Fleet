using Fleet.Blazor.Security;
using Fleet.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Blazor.Controllers;

[Route("api/security/audit")]
[ApiController]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILocalSessionValidator _localSessionValidator;

    public AuditController(IAuditRepository auditRepository, ILocalSessionValidator localSessionValidator)
    {
        _auditRepository = auditRepository;
        _localSessionValidator = localSessionValidator;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<AuditRecordQueryResult>>> GetRecent([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        if (!_localSessionValidator.IsAuthorized(HttpContext))
        {
            return Unauthorized();
        }

        var normalizedLimit = Math.Clamp(limit, 1, 200);
        var records = await _auditRepository.GetRecentAsync(normalizedLimit, cancellationToken);
        return Ok(records);
    }
}
