using Fleet.Blazor.Security;
using Fleet.Shared;

namespace Fleet.Blazor.Tests.Security;

public class PermissionPolicyServiceTests
{
    private readonly PermissionPolicyService _sut = new();

    [Fact]
    public void Evaluate_DeniesUnknownCaller()
    {
        var identity = new RequestIdentity(RequestSourceType.UnknownLocalCaller, "unknown", "corr-1");
        var action = new ActionDescriptor(ActionType.CredentialWrite, "FLEET_AZURE_MODEL_KEY", RiskLevel.High, identity.RequestedBy);

        var result = _sut.Evaluate(action, identity);

        Assert.Equal(PolicyDecision.Deny, result.Decision);
    }

    [Fact]
    public void Evaluate_RequiresConsent_ForBrowserExtensionCredentialMutation()
    {
        var identity = new RequestIdentity(RequestSourceType.BrowserExtension, "browser-extension", "corr-1");
        var action = new ActionDescriptor(ActionType.CredentialDelete, "FLEET_AZURE_MODEL_KEY", RiskLevel.High, identity.RequestedBy);

        var result = _sut.Evaluate(action, identity);

        Assert.Equal(PolicyDecision.RequireInteractiveConsent, result.Decision);
    }

    [Fact]
    public void Evaluate_AllowsTrustedBlazorCredentialWrite()
    {
        var identity = new RequestIdentity(RequestSourceType.BlazorUiInteractive, "blazor-ui", "corr-1");
        var action = new ActionDescriptor(ActionType.CredentialWrite, "FLEET_AZURE_MODEL_KEY", RiskLevel.High, identity.RequestedBy);

        var result = _sut.Evaluate(action, identity);

        Assert.Equal(PolicyDecision.Allow, result.Decision);
    }

    [Fact]
    public void Evaluate_RequiresConsent_ForBrowserExtensionRuntimeExecution()
    {
        var identity = new RequestIdentity(RequestSourceType.BrowserExtension, "browser-extension", "corr-2");
        var action = new ActionDescriptor(ActionType.NetworkEgress, "chat-completions:run-task", RiskLevel.High, identity.RequestedBy);

        var result = _sut.Evaluate(action, identity);

        Assert.Equal(PolicyDecision.RequireInteractiveConsent, result.Decision);
    }

    [Fact]
    public void Evaluate_AllowsRuntimeExecution_ForTrustedBlazorCaller()
    {
        var identity = new RequestIdentity(RequestSourceType.BlazorUiInteractive, "blazor-ui", "corr-3");
        var action = new ActionDescriptor(ActionType.NetworkEgress, "chat-completions:run-task", RiskLevel.High, identity.RequestedBy);

        var result = _sut.Evaluate(action, identity);

        Assert.Equal(PolicyDecision.Allow, result.Decision);
    }
}
