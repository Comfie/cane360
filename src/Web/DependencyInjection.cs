using Cane360.Application.Common.Interfaces;
using Cane360.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();
        builder.Services.AddScoped<IDatabaseHealthCheck, DatabaseHealthCheck>();
        builder.Services.AddScoped<DatabaseStatusReporter>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler(options => { });
        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddControllers();

        builder.Services.AddOpenApi(options =>
            options.AddOperationTransformer<ApiExceptionOperationTransformer>());
    }

}
