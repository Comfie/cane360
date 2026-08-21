using Cane360.Infrastructure;
using Cane360.Web.Infrastructure;
using Cane360.Web.Services;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    bool isOpenApiDocumentGeneration = OpenApiDocumentGeneration.IsRequested();

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddJsonFile(
            "appsettings.Development.Local.json",
            optional: true,
            reloadOnChange: true);
    }

    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Cane360"));

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

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "Cane360 HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = static (httpContext, elapsed, exception) =>
            RequestLogLevel.Select(httpContext.Response.StatusCode, elapsed, exception);
    });

    app.MapOpenApi();
    app.MapScalarApiReference();

app.UseExceptionHandler();

    app.UseAuthentication();
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
