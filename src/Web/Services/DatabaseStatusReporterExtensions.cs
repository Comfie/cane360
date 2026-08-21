using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Web.Services;

public static class DatabaseStatusReporterExtensions
{
    public static async Task<int> ReportDatabaseStatusAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var reporter = scope.ServiceProvider.GetRequiredService<DatabaseStatusReporter>();

        return await reporter.ReportAsync(cancellationToken);
    }
}
