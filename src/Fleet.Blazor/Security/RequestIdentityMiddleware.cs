using Fleet.Shared;

namespace Fleet.Blazor.Security;

public sealed class RequestIdentityMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string CallerHeader = "X-Fleet-Caller";

    private readonly RequestDelegate _next;

    public RequestIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, RequestIdentityContext identityContext)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        var caller = context.Request.Headers[CallerHeader].FirstOrDefault();
        var source = caller?.ToLowerInvariant() switch
        {
            "blazor-ui" => RequestSourceType.BlazorUiInteractive,
            "browser-extension" => RequestSourceType.BrowserExtension,
            "internal-system" => RequestSourceType.InternalSystem,
            _ => RequestSourceType.UnknownLocalCaller,
        };

        identityContext.Current = new RequestIdentity(
            Source: source,
            RequestedBy: string.IsNullOrWhiteSpace(caller) ? "unknown" : caller,
            CorrelationId: correlationId);

        context.Response.Headers[CorrelationHeader] = correlationId;
        await _next(context);
    }
}
