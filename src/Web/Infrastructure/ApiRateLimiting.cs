using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cane360.Web.Infrastructure;

public static class ApiRateLimiting
{
    public static void AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ApiRateLimitOptions limits = configuration.GetSection(ApiRateLimitOptions.SectionName)
            .Get<ApiRateLimitOptions>() ?? new ApiRateLimitOptions();
        if (limits.UsersPermitLimit < 1 || limits.UsersWindowSeconds < 1 ||
            limits.ExportsPermitLimit < 1 || limits.ExportsWindowSeconds < 1)
        {
            throw new InvalidOperationException("Rate-limiting permits and windows must be positive.");
        }

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(ApiRateLimitOptions.UsersPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter("all-users", _ =>
                    Window(limits.UsersPermitLimit, limits.UsersWindowSeconds)));
            options.AddPolicy(ApiRateLimitOptions.ExportsPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ExportKey(context), _ =>
                    Window(limits.ExportsPermitLimit, limits.ExportsWindowSeconds)));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                HttpContext context = rejected.HttpContext;
                string? policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                int fallbackSeconds = policy == ApiRateLimitOptions.ExportsPolicy
                    ? limits.ExportsWindowSeconds
                    : limits.UsersWindowSeconds;
                TimeSpan retryAfter = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter,
                    out TimeSpan delay)
                    ? delay
                    : TimeSpan.FromSeconds(fallbackSeconds);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.RetryAfter = Math.Max(1,
                    (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                ProblemDetails problem = new()
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Type = "https://www.rfc-editor.org/rfc/rfc6585#section-4",
                    Title = "Too many requests.",
                    Detail = "Try again after the Retry-After interval."
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;
                await context.Response.WriteAsJsonAsync(problem, cancellationToken);
            };
        });
    }

    private static FixedWindowRateLimiterOptions Window(int permits, int seconds)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits, Window = TimeSpan.FromSeconds(seconds), QueueLimit = 0, AutoReplenishment = true
        };
    }

    private static string ExportKey(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId
            ? $"user:{userId}"
            : "anonymous";
    }
}
