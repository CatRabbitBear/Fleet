using Fleet.Blazor.Controllers;
using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
using Fleet.Runtime.Agents;
using Fleet.Runtime.Contracts;
using Fleet.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fleet.Blazor.Tests.Security;

public class ChatCompletionsControllerSecurityTests
{
    [Fact]
    public async Task RunTask_ReturnsUnauthorized_WhenLocalSessionIsInvalid()
    {
        var runner = new FakeRunner();
        var executor = new FakePrivilegedExecutor();
        var controller = CreateController(
            runner,
            executor,
            new AlwaysUnauthorizedSessionValidator(),
            new RequestIdentityContext(),
            new FakeAgentRepository());

        var result = await controller.RunTask(new AgentRequest(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.False(runner.WasCalled);
    }

    [Fact]
    public async Task RunTask_ReturnsForbidden_WhenPolicyDeniesRequest()
    {
        var runner = new FakeRunner();
        var executor = new FakePrivilegedExecutor
        {
            AuthorizeResult = (false, new PolicyEvaluationResult(PolicyDecision.Deny, "Denied"), "PolicyDenied")
        };

        var controller = CreateController(
            runner,
            executor,
            new AlwaysAuthorizedSessionValidator(),
            new RequestIdentityContext
            {
                Current = new RequestIdentity(RequestSourceType.UnknownLocalCaller, "unknown", "corr-deny")
            },
            new FakeAgentRepository());

        var result = await controller.RunTask(new AgentRequest { AgentId = FakeAgentRepository.DefaultAgentId }, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        var objectResult = (ObjectResult)result.Result!;
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.False(runner.WasCalled);
        Assert.True(executor.AuditCalled);
    }

    [Fact]
    public async Task RunTask_ReturnsOk_AndAuditsSuccess_WhenAuthorized()
    {
        var runner = new FakeRunner();
        var executor = new FakePrivilegedExecutor();
        var identity = new RequestIdentityContext
        {
            Current = new RequestIdentity(RequestSourceType.BlazorUiInteractive, "blazor-ui", "corr-success")
        };

        var controller = CreateController(
            runner,
            executor,
            new AlwaysAuthorizedSessionValidator(),
            identity,
            new FakeAgentRepository());

        var request = new AgentRequest
        {
            AgentId = FakeAgentRepository.DefaultAgentId,
            History =
            [
                new AgentRequestItem { Role = MessageType.User, Content = "hello" }
            ]
        };

        var result = await controller.RunTask(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AgentResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.RunId));
        Assert.True(runner.WasCalled);
        Assert.True(executor.AuditCalled);
        Assert.Equal("Success", executor.LastOutcome);
        Assert.Equal("blazor-ui", executor.LastAction?.RequestedBy);
    }

    [Fact]
    public async Task RunTask_ReturnsBadRequest_AndAuditsExecutionFailure_WhenRunnerThrows()
    {
        var runner = new FakeRunner { ThrowOnRun = true };
        var executor = new FakePrivilegedExecutor();

        var controller = CreateController(
            runner,
            executor,
            new AlwaysAuthorizedSessionValidator(),
            new RequestIdentityContext
            {
                Current = new RequestIdentity(RequestSourceType.BlazorUiInteractive, "blazor-ui", "corr-fail")
            },
            new FakeAgentRepository());

        var result = await controller.RunTask(new AgentRequest { AgentId = FakeAgentRepository.DefaultAgentId }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.True(runner.WasCalled);
        Assert.True(executor.AuditCalled);
        Assert.Equal("ExecutionFailure", executor.LastOutcome);
    }

    private static ChatCompletionsController CreateController(
        IChatCompletionsRunner runner,
        IPrivilegedActionExecutor privilegedExecutor,
        ILocalSessionValidator sessionValidator,
        RequestIdentityContext identityContext,
        IAgentGovernanceRepository agentRepository)
    {
        var controller = new ChatCompletionsController(
            NullLogger<ChatCompletionsController>.Instance,
            runner,
            privilegedExecutor,
            sessionValidator,
            identityContext,
            agentRepository)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private sealed class FakeRunner : IChatCompletionsRunner
    {
        public bool WasCalled { get; private set; }
        public bool ThrowOnRun { get; set; }

        public Task<AgentResponse> RunTaskAsync(List<AgentRequestItem> history)
        {
            WasCalled = true;
            if (ThrowOnRun)
            {
                throw new InvalidOperationException("runner failed");
            }

            return Task.FromResult(new AgentResponse { Result = "ok", FilePath = "path" });
        }
    }

    private sealed class FakePrivilegedExecutor : IPrivilegedActionExecutor
    {
        public (bool Allowed, PolicyEvaluationResult Policy, string Outcome) AuthorizeResult { get; set; }
            = (true, new PolicyEvaluationResult(PolicyDecision.Allow, "ok"), "Authorized");

        public bool AuditCalled { get; private set; }
        public ActionDescriptor? LastAction { get; private set; }
        public string? LastOutcome { get; private set; }

        public Task<(bool Allowed, PolicyEvaluationResult Policy, string Outcome)> AuthorizeAsync(ActionDescriptor action, CancellationToken cancellationToken)
        {
            LastAction = action;
            return Task.FromResult(AuthorizeResult);
        }

        public Task AuditAsync(ActionDescriptor action, PolicyEvaluationResult policy, string outcome, CancellationToken cancellationToken)
        {
            AuditCalled = true;
            LastAction = action;
            LastOutcome = outcome;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAgentRepository : IAgentGovernanceRepository
    {
        public const string DefaultAgentId = "agent-1";

        public Task AddArtifactAsync(string runId, string type, string storagePath, string checksum, long contentSize, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CompleteRunAsync(string runId, string status, string? error, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<AgentDefinitionRecord> CreateAgentAsync(AgentUpsertCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task EnsureInitializedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<AgentDefinitionRecord?> GetAgentAsync(string agentId, CancellationToken cancellationToken)
            => Task.FromResult<AgentDefinitionRecord?>(
                agentId == DefaultAgentId
                    ? new AgentDefinitionRecord(DefaultAgentId, "Test", "Desc", true, "v1", ExtensionPermissionTier.Medium, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
                    : null);

        public Task<AgentDefinitionVersionRecord?> GetActiveVersionAsync(string agentId, CancellationToken cancellationToken) => Task.FromResult<AgentDefinitionVersionRecord?>(null);
        public Task<IReadOnlyList<AgentDefinitionRecord>> ListAgentsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentDefinitionRecord>>([]);
        public Task<IReadOnlyList<AgentArtifactRecord>> ListArtifactsByRunAsync(string runId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentArtifactRecord>>([]);
        public Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(string? agentId, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentRunRecord>>([]);
        public Task<string> StartRunAsync(string agentId, string correlationId, string initiatedBy, string source, string policyDecision, CancellationToken cancellationToken) => Task.FromResult(Guid.NewGuid().ToString("N"));
    }

    private sealed class AlwaysUnauthorizedSessionValidator : ILocalSessionValidator
    {
        public bool IsAuthorized(HttpContext context) => false;
    }

    private sealed class AlwaysAuthorizedSessionValidator : ILocalSessionValidator
    {
        public bool IsAuthorized(HttpContext context) => true;
    }
}
