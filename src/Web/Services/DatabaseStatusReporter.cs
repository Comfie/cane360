using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Web.Services;

public sealed class DatabaseStatusReporter(
    ApplicationDbContext context,
    IHostEnvironment environment,
    ILogger<DatabaseStatusReporter> logger)
{
    public async Task<int> ReportAsync(CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var provider = context.Database.ProviderName ?? "Unknown";

        logger.LogInformation(
            "Database target: Environment={Environment}, Provider={Provider}, Server={Server}, Database={Database}",
            environment.EnvironmentName,
            provider,
            connection.DataSource,
            connection.Database);

        try
        {
            if (!await context.Database.CanConnectAsync(cancellationToken))
            {
                logger.LogError("Database connection failed. No migrations were applied.");
                return 1;
            }

            var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();

            logger.LogInformation(
                "Database connection succeeded. Applied migrations: {AppliedCount}. Pending migrations: {PendingCount}.",
                appliedMigrations.Length,
                pendingMigrations.Length);

            foreach (var migration in pendingMigrations)
            {
                logger.LogWarning("Pending migration: {Migration}", migration);
            }

            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Database status check failed with {ErrorType}. No migrations were applied.",
                exception.GetType().Name);
            return 1;
        }
    }
}
