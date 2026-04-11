using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
using Fleet.Runtime.Security;
using Fleet.Shared;

namespace Fleet.Blazor.Tests.Security;

public class RuntimeExecutionGateTests
{
    [Fact]
    public async Task AuthorizeAsync_MapsFileWrite_ToPrivilegedExecutor()
    {
        var executor = new FakePrivilegedActionExecutor();
        var sut = new RuntimeExecutionGate(executor);

        var decision = await sut.AuthorizeAsync(new RuntimeExecutionRequest(
            Action: "file:write",
            Resource: "C:/temp/a.txt",
            Risk: RuntimeExecutionRisk.High,
            RequestedBy: "runtime-agent"));

        Assert.True(decision.Allowed);
        Assert.Equal(ActionType.FileWrite, executor.LastAction?.ActionType);
        Assert.Equal(RiskLevel.High, executor.LastAction?.RiskLevel);
    }

    private sealed class FakePrivilegedActionExecutor : IPrivilegedActionExecutor
    {
        public ActionDescriptor? LastAction { get; private set; }

        public Task<(bool Allowed, PolicyEvaluationResult Policy, string Outcome)> AuthorizeAsync(ActionDescriptor action, CancellationToken cancellationToken)
        {
            LastAction = action;
            return Task.FromResult((true, new PolicyEvaluationResult(PolicyDecision.Allow, "ok"), "Authorized"));
        }

        public Task AuditAsync(ActionDescriptor action, PolicyEvaluationResult policy, string outcome, CancellationToken cancellationToken)
        {
            LastAction = action;
            return Task.CompletedTask;
        }
    }
}
