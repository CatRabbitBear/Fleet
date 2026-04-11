using Fleet.Runtime.Security;
using Fleet.Blazor.Services;
using Fleet.Shared;

namespace Fleet.Blazor.Security;

public sealed class RuntimeExecutionGate : IRuntimeExecutionGate
{
    private readonly IPrivilegedActionExecutor _privilegedActionExecutor;

    public RuntimeExecutionGate(IPrivilegedActionExecutor privilegedActionExecutor)
    {
        _privilegedActionExecutor = privilegedActionExecutor;
    }

    public async Task<RuntimeExecutionDecision> AuthorizeAsync(RuntimeExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var action = BuildActionDescriptor(request);
        var (allowed, policy, outcome) = await _privilegedActionExecutor.AuthorizeAsync(action, cancellationToken);
        return new RuntimeExecutionDecision(allowed, outcome, policy.Rationale);
    }

    public Task AuditAsync(RuntimeExecutionRequest request, RuntimeExecutionDecision decision, string finalOutcome, CancellationToken cancellationToken = default)
    {
        var action = BuildActionDescriptor(request);
        var policy = new PolicyEvaluationResult(
            decision.Allowed ? PolicyDecision.Allow : PolicyDecision.Deny,
            decision.Rationale);

        return _privilegedActionExecutor.AuditAsync(action, policy, finalOutcome, cancellationToken);
    }

    private static ActionDescriptor BuildActionDescriptor(RuntimeExecutionRequest request)
    {
        return new ActionDescriptor(
            ResolveActionType(request.Action),
            request.Resource,
            ResolveRisk(request.Risk),
            request.RequestedBy);
    }

    private static ActionType ResolveActionType(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "process:spawn" => ActionType.ProcessSpawn,
            "file:write" => ActionType.FileWrite,
            _ => ActionType.NetworkEgress
        };
    }

    private static RiskLevel ResolveRisk(RuntimeExecutionRisk risk)
    {
        return risk switch
        {
            RuntimeExecutionRisk.Low => RiskLevel.Low,
            RuntimeExecutionRisk.Medium => RiskLevel.Medium,
            _ => RiskLevel.High
        };
    }
}
