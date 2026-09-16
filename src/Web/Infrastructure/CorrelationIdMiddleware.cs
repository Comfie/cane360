using Microsoft.Extensions.Primitives;

namespace Cane360.Web.Infrastructure;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = Resolve(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }

    private static string Resolve(StringValues suppliedValues)
    {
        string? supplied = suppliedValues.Count == 1 ? suppliedValues[0]?.Trim() : null;

        if (supplied is not null && supplied.Length is > 0 and <= 64 && supplied.All(IsSafeCharacter))
        {
            return supplied;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsSafeCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_' or '.';
}
