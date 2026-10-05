using Cane360.Infrastructure.Data;

namespace Cane360.Web.Services;

public sealed class DatabaseHealthCheck(ApplicationDbContext context) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return context.Database.CanConnectAsync(cancellationToken);
    }
}
