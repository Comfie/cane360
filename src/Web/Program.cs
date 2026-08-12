using System.Reflection;
using Cane360.Infrastructure;
using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

if (ShouldInitialiseDatabase())
{
    await app.InitialiseDatabaseAsync();
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

static bool ShouldInitialiseDatabase()
{
    if (EF.IsDesignTime)
    {
        return false;
    }

    var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

    return entryAssemblyName is not "GetDocument.Insider" and not "dotnet-getdocument";
}
