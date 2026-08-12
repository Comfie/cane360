using Cane360.Infrastructure;
using Cane360.Web.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.AddInfrastructureServices();
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
app.UseCors(static policy =>
    policy.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
