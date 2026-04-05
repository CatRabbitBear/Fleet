using Fleet.Shared;

namespace Fleet.Blazor.Security;

public sealed class RequestIdentityContext
{
    public RequestIdentity Current { get; set; } = new(RequestSourceType.InternalSystem, "system", Guid.NewGuid().ToString("N"));
}
