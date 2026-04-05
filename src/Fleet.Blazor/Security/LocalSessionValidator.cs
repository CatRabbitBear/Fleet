namespace Fleet.Blazor.Security;

public sealed class LocalSessionOptions
{
    public string SessionToken { get; set; } = string.Empty;
}

public interface ILocalSessionValidator
{
    bool IsAuthorized(HttpContext context);
}

public sealed class LocalSessionValidator : ILocalSessionValidator
{
    private const string Header = "X-Fleet-Session-Token";
    private readonly LocalSessionOptions _options;

    public LocalSessionValidator(LocalSessionOptions options)
    {
        _options = options;
    }

    public bool IsAuthorized(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.SessionToken))
        {
            return false;
        }

        var candidate = context.Request.Headers[Header].FirstOrDefault();
        return string.Equals(candidate, _options.SessionToken, StringComparison.Ordinal);
    }
}
