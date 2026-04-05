using Fleet.Blazor.Agents;
using Fleet.Blazor.Controllers;
using Fleet.Blazor.Pipeline.Dtos;
using Fleet.Blazor.Security;
using Fleet.Blazor.Services;
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
            new RequestIdentityContext());

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
            });

        var result = await controller.RunTask(new AgentRequest(), CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        var objectResult = (ObjectResult)result.Result!;
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.False(runner.WasCalled);
        Assert.True(executor.AuditCalled);
    }

    private static ChatCompletionsController CreateController(
        IChatCompletionsRunner runner,
        IPrivilegedActionExecutor privilegedExecutor,
        ILocalSessionValidator sessionValidator,
        RequestIdentityContext identityContext)
    {
        var controller = new ChatCompletionsController(
            NullLogger<ChatCompletionsController>.Instance,
            runner,
            privilegedExecutor,
            sessionValidator,
            identityContext)
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

        public Task<AgentResponse> RunTaskAsync(List<AgentRequestItem> history)
        {
            WasCalled = true;
            return Task.FromResult(new AgentResponse { Result = "ok", FilePath = "path" });
        }
    }

    private sealed class FakePrivilegedExecutor : IPrivilegedActionExecutor
    {
        public (bool Allowed, PolicyEvaluationResult Policy, string Outcome) AuthorizeResult { get; set; }
            = (true, new PolicyEvaluationResult(PolicyDecision.Allow, "ok"), "Authorized");

        public bool AuditCalled { get; private set; }

        public Task<(bool Allowed, PolicyEvaluationResult Policy, string Outcome)> AuthorizeAsync(ActionDescriptor action, CancellationToken cancellationToken)
            => Task.FromResult(AuthorizeResult);

        public Task AuditAsync(ActionDescriptor action, PolicyEvaluationResult policy, string outcome, CancellationToken cancellationToken)
        {
            AuditCalled = true;
            return Task.CompletedTask;
        }
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
