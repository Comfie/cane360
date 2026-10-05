namespace Cane360.Web.Services;

public static class DatabaseStatusReporterExtensions
{
    public static async Task<int> ReportDatabaseStatusAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        DatabaseStatusReporter reporter = scope.ServiceProvider.GetRequiredService<DatabaseStatusReporter>();

        return await reporter.ReportAsync(cancellationToken);
    }
}
