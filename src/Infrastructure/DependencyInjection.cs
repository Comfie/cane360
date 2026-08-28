using Cane360.Application.Common.Interfaces;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Data.Interceptors;
using Cane360.Infrastructure.Identity;
using Cane360.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cane360.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(
        this IHostApplicationBuilder builder,
        bool validateNationalIdOnStart = true)
    {
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, AppendOnlyEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = builder.Configuration.GetConnectionString("Cane360Db");
            Guard.Against.NullOrWhiteSpace(
                connectionString,
                message: "Connection string 'Cane360Db' not found.");

            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<IFarmSetupRepository, FarmSetupRepository>();
        builder.Services.AddScoped<ILabourRepository, LabourRepository>();
        builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
        builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
        OptionsBuilder<NationalIdProtectionOptions> nationalIdOptions = builder.Services
            .AddOptions<NationalIdProtectionOptions>()
            .Bind(builder.Configuration.GetSection(NationalIdProtectionOptions.SectionName));
        if (validateNationalIdOnStart)
        {
            nationalIdOptions.ValidateOnStart();
        }
        builder.Services.AddSingleton<IValidateOptions<NationalIdProtectionOptions>, NationalIdProtectionOptionsValidator>();
        builder.Services.AddSingleton<IWorkerSensitiveDataProtector, WorkerSensitiveDataProtector>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
    }
}
