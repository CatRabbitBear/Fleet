using Fleet.Shared;

namespace Fleet.Blazor.Security;

public interface IPermissionPolicyService
{
    PolicyEvaluationResult Evaluate(ActionDescriptor action, RequestIdentity identity);
}

public sealed class PermissionPolicyService : IPermissionPolicyService
{
    public PolicyEvaluationResult Evaluate(ActionDescriptor action, RequestIdentity identity)
    {
        if (identity.Source == RequestSourceType.UnknownLocalCaller)
        {
            return new PolicyEvaluationResult(PolicyDecision.Deny, "Unknown caller is denied by default.");
        }

        if (action.ActionType is ActionType.CredentialWrite or ActionType.CredentialDelete)
        {
            if (identity.Source == RequestSourceType.BrowserExtension)
            {
                return new PolicyEvaluationResult(PolicyDecision.RequireInteractiveConsent, "Browser extension credential mutation requires interactive approval.");
            }

            return new PolicyEvaluationResult(PolicyDecision.Allow, "Credential mutation allowed for trusted source.");
        }

        if (action.ActionType is ActionType.ProcessSpawn or ActionType.FileWrite or ActionType.NetworkEgress)
        {
            if (identity.Source == RequestSourceType.BrowserExtension)
            {
                return new PolicyEvaluationResult(PolicyDecision.RequireInteractiveConsent, "Browser extension runtime actions require interactive approval.");
            }

            return new PolicyEvaluationResult(PolicyDecision.Allow, "Runtime action permitted for trusted local session.");
        }

        return new PolicyEvaluationResult(PolicyDecision.Allow, "Action permitted by baseline policy.");
    }
}
