using Fleet.Blazor.Security;
using Microsoft.AspNetCore.Http;

namespace Fleet.Blazor.Tests.Security;

public class LocalSessionValidatorTests
{
    [Fact]
    public void IsAuthorized_ReturnsFalse_WhenHeaderMissing()
    {
        var options = new LocalSessionOptions { SessionToken = "abc123" };
        var sut = new LocalSessionValidator(options);
        var context = new DefaultHttpContext();

        var result = sut.IsAuthorized(context);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorized_ReturnsTrue_WhenHeaderMatches()
    {
        var options = new LocalSessionOptions { SessionToken = "abc123" };
        var sut = new LocalSessionValidator(options);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Fleet-Session-Token"] = "abc123";

        var result = sut.IsAuthorized(context);

        Assert.True(result);
    }
}
