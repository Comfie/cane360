using Cane360.Infrastructure;
using Cane360.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Web.UnitTests.Infrastructure;

public class OpenApiAndSecurityStartupTests
{
    [Test]
    public async Task NormalRuntimeStartupFailsWithoutNationalIdConfiguration()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddInfrastructureServices();
        using IHost host = builder.Build();

        await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Test]
    public async Task NormalRuntimeStartupSucceedsWithValidNationalIdConfiguration()
    {
        HostApplicationBuilder builder = ValidBuilder();
        builder.AddInfrastructureServices();
        using IHost host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Test]
    public async Task DocumentGenerationStartupDoesNotRequireOperationalSecrets()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddInfrastructureServices(validateNationalIdOnStart: false);
        using IHost host = builder.Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Test]
    public void DocumentGenerationExemptionRequiresExactHostAssembly()
    {
        OpenApiDocumentGeneration.IsRequested("GetDocument.Insider").ShouldBeTrue();
        OpenApiDocumentGeneration.IsRequested("Cane360.Web").ShouldBeFalse();
        OpenApiDocumentGeneration.IsRequested("getdocument.insider").ShouldBeFalse();
        OpenApiDocumentGeneration.IsRequested(null).ShouldBeFalse();
    }

    private static HostApplicationBuilder ValidBuilder()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration["Cane360Security:NationalId:ActiveKeyId"] = "test-v1";
        builder.Configuration["Cane360Security:NationalId:Keys:test-v1"] = Encode(1);
        builder.Configuration["Cane360Security:NationalId:FingerprintKey"] = Encode(33);
        return builder;
    }

    private static string Encode(int start) =>
        Convert.ToBase64String(Enumerable.Range(start, 32).Select(value => (byte)value).ToArray());
}
