using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Cane360.Web.Infrastructure;
using Cane360.Web.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Web.UnitTests.Infrastructure;

public sealed class ApiRateLimitingIntegrationTests
{
    [Test]
    public void TargetActionsCarryTheConfiguredPolicies()
    {
        typeof(UsersController).GetCustomAttribute<EnableRateLimitingAttribute>()!
            .PolicyName.ShouldBe(ApiRateLimitOptions.UsersPolicy);

        (Type Controller, string Action)[] exports =
        [
            (typeof(AdministrationController), nameof(AdministrationController.ExportAudit)),
            (typeof(InventoryController), nameof(InventoryController.LeakageReport)),
            (typeof(InventoryController), nameof(InventoryController.ExportLeakageReport)),
            (typeof(MillRecordsController), nameof(MillRecordsController.ExportTickets)),
            (typeof(MillRecordsController), nameof(MillRecordsController.ExportStatements)),
            (typeof(FinanceController), nameof(FinanceController.GetBudgetVariance))
        ];

        foreach (var (controller, action) in exports)
        {
            controller.GetMethod(action)!.GetCustomAttribute<EnableRateLimitingAttribute>()!
                .PolicyName.ShouldBe(ApiRateLimitOptions.ExportsPolicy);
        }
    }

    [Test]
    public async Task UsersLimitTripsAcrossActionsAndRecoversAfterWindow()
    {
        await using var app = CreateApp();
        app.MapGet("/api/users/login", () => Results.Ok()).RequireRateLimiting(ApiRateLimitOptions.UsersPolicy);
        app.MapGet("/api/users/register", () => Results.Ok()).RequireRateLimiting(ApiRateLimitOptions.UsersPolicy);
        await app.StartAsync();
        using var client = Client(app);

        using var first = await client.GetAsync("/api/users/login");
        using var rejected = await client.GetAsync("/api/users/register");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertRejectionAsync(rejected);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        using var recovered = await client.GetAsync("/api/users/register");
        recovered.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ExportLimitPartitionsByAuthenticatedUserAndRecovers()
    {
        await using var app = CreateApp();
        app.MapGet("/api/inventory/leakage-report.csv", () => Results.Ok())
            .RequireRateLimiting(ApiRateLimitOptions.ExportsPolicy);
        await app.StartAsync();
        using var client = Client(app);

        using var first = await GetExportAsync(client, "user-a");
        using var rejected = await GetExportAsync(client, "user-a");
        using var otherUser = await GetExportAsync(client, "user-b");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertRejectionAsync(rejected);
        otherUser.StatusCode.ShouldBe(HttpStatusCode.OK);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        using var recovered = await GetExportAsync(client, "user-a");
        recovered.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:UsersPermitLimit"] = "1",
            ["RateLimiting:UsersWindowSeconds"] = "1",
            ["RateLimiting:ExportsPermitLimit"] = "1",
            ["RateLimiting:ExportsWindowSeconds"] = "1"
        });
        builder.Services.AddApiRateLimiting(builder.Configuration);
        var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Test-User", out var userId))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
            }
            await next();
        });
        app.UseRateLimiter();
        return app;
    }

    private static HttpClient Client(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static Task<HttpResponseMessage> GetExportAsync(HttpClient client, string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/inventory/leakage-report.csv");
        request.Headers.Add("X-Test-User", userId);
        return client.SendAsync(request);
    }

    private static async Task AssertRejectionAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").ShouldBeTrue();
        int.TryParse(response.Headers.GetValues("Retry-After").Single(), out int seconds).ShouldBeTrue();
        seconds.ShouldBeGreaterThan(0);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(429);
        body.RootElement.GetProperty("type").GetString()
            .ShouldBe("https://www.rfc-editor.org/rfc/rfc6585#section-4");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
