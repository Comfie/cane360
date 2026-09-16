using Cane360.Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cane360.Web.UnitTests.Infrastructure;

public class CorrelationIdMiddlewareTests
{
    [Test]
    public async Task InvokeAsyncPropagatesValidatedCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "support-reference_123";
        string? observed = null;
        var middleware = new CorrelationIdMiddleware(nextContext =>
        {
            observed = nextContext.TraceIdentifier;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        observed.ShouldBe("support-reference_123");
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe("support-reference_123");
    }

    [TestCase("contains spaces")]
    [TestCase("line\nbreak")]
    [TestCase("one,two")]
    public async Task InvokeAsyncReplacesUnsafeCorrelationId(string supplied)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = supplied;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.ShouldNotBe(supplied);
        context.TraceIdentifier.Length.ShouldBe(32);
        context.TraceIdentifier.All(char.IsAsciiHexDigit).ShouldBeTrue();
    }

    [Test]
    public async Task InvokeAsyncReplacesAmbiguousMultipleCorrelationIds()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = new StringValues(["first", "second"]);
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.ShouldNotBe("first");
        context.TraceIdentifier.ShouldNotBe("second");
    }
}
