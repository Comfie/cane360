using System.Reflection;
using Cane360.Application.Administration;
using Cane360.Application.Common.Behaviours;
using Cane360.Application.Finance;
using Cane360.Application.MillRecords;
using Cane360.Application.Payroll;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cane360.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddScoped<IPayrollSettlementService, PayrollSettlementService>();
        builder.Services.AddScoped<IFinanceService, FinanceService>();
        builder.Services.AddScoped<IPayrollCostProjectionService, PayrollCostProjectionService>();
        builder.Services.AddScoped<IMillRecordsService, MillRecordsService>();
        builder.Services.AddScoped<AdministrationService>();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenRequestPreProcessor(typeof(LoggingBehaviour<>));
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
        });
    }
}
