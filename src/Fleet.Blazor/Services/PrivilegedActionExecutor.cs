using Fleet.Blazor.Security;
using Fleet.Shared;

namespace Fleet.Blazor.Services;

public interface IPrivilegedActionExecutor
{
    Task<(bool Allowed, PolicyEvaluationResult Policy, string Outcome)> AuthorizeAsync(ActionDescriptor action, CancellationToken cancellationToken);
    Task AuditAsync(ActionDescriptor action, PolicyEvaluationResult policy, string outcome, CancellationToken cancellationToken);
}

public sealed class PrivilegedActionExecutor : IPrivilegedActionExecutor
{
    private readonly RequestIdentityContext _identityContext;
    private readonly IPermissionPolicyService _policyService;
    private readonly IConsentService _consentService;
    private readonly IAuditRepository _auditRepository;

    public PrivilegedActionExecutor(
        RequestIdentityContext identityContext,
        IPermissionPolicyService policyService,
        IConsentService consentService,
        IAuditRepository auditRepository)
    {
        _identityContext = identityContext;
        _policyService = policyService;
        _consentService = consentService;
        _auditRepository = auditRepository;
    }

    public async Task<(bool Allowed, PolicyEvaluationResult Policy, string Outcome)> AuthorizeAsync(ActionDescriptor action, CancellationToken cancellationToken)
    {
        var identity = _identityContext.Current;
        var policy = _policyService.Evaluate(action, identity);
        if (policy.Decision == PolicyDecision.Deny)
        {
            return (false, policy, "PolicyDenied");
        }

        if (policy.Decision == PolicyDecision.RequireInteractiveConsent)
        {
            var granted = await _consentService.RequestConsentAsync(action, identity, cancellationToken);
            if (!granted)
            {
                return (false, policy, "ConsentDeniedOrTimeout");
            }
        }

        return (true, policy, "Authorized");
    }

    public Task AuditAsync(ActionDescriptor action, PolicyEvaluationResult policy, string outcome, CancellationToken cancellationToken)
    {
        var identity = _identityContext.Current;
        var record = new AuditRecord(
            TimestampUtc: DateTimeOffset.UtcNow,
            CorrelationId: identity.CorrelationId,
            Source: identity.Source,
            RequestedBy: identity.RequestedBy,
            ActionType: action.ActionType,
            Resource: action.Resource,
            RiskLevel: action.RiskLevel,
            PolicyDecision: policy.Decision,
            FinalOutcome: outcome,
            Rationale: policy.Rationale);

        return _auditRepository.WriteAsync(record, cancellationToken);
    }
}
