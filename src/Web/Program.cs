using Cane360.Infrastructure;
using Cane360.Web.Infrastructure;
using Cane360.Web.Services;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = ConfigureConsole(new LoggerConfiguration(),
        string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Environments.Development, StringComparison.OrdinalIgnoreCase))
    .CreateBootstrapLogger();

try
{
    bool isOpenApiDocumentGeneration = OpenApiDocumentGeneration.IsRequested();
    if (isOpenApiDocumentGeneration)
    {
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
    }

    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddJsonFile(
            "appsettings.Development.Local.json",
            optional: true,
            reloadOnChange: true);
    }

    builder.Services.AddSerilog((services, loggerConfiguration) => ConfigureConsole(
        loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Cane360"),
        builder.Environment.IsDevelopment()));

    string? portValue = Environment.GetEnvironmentVariable("PORT");

    if (!string.IsNullOrWhiteSpace(portValue))
    {
        if (!int.TryParse(portValue, out int port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("PORT must be a valid TCP port number.");
        }

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));
    }

    builder.AddApplicationServices();
    builder.AddInfrastructureServices(validateNationalIdOnStart: !isOpenApiDocumentGeneration);
    builder.AddWebServices();

    var app = builder.Build();

    if (app.Environment.IsProduction())
    {
        app.Logger.LogWarning("Anonymous API rate limiting is shared across callers because the deployment does not have a trusted client-IP forwarding configuration.");
    }

    if (args.Contains("--database-status", StringComparer.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Cane360Db")))
        {
            app.Logger.LogError("Database status was not checked because ConnectionStrings:Cane360Db is not configured.");
            Environment.ExitCode = 1;
            return;
        }

        Environment.ExitCode = await app.ReportDatabaseStatusAsync();
        return;
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseFileServer();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "Cane360 HTTP {RequestMethod} {EndpointRoute} responded {StatusCode} in {Elapsed:0.0000} ms; reference {CorrelationId}";
        options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("EndpointRoute",
                (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched");
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        };
        options.GetLevel = static (httpContext, elapsed, exception) =>
            RequestLogLevel.Select(httpContext.Response.StatusCode, elapsed, exception);
    });

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseExceptionHandler();

    app.UseRouting();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();

    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Cane360 terminated unexpectedly during startup or shutdown.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static LoggerConfiguration ConfigureConsole(LoggerConfiguration configuration, bool isDevelopment) =>
    isDevelopment
        ? configuration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code,
            applyThemeToRedirectedOutput: true)
        : configuration.WriteTo.Console(new RailwayJsonFormatter());
