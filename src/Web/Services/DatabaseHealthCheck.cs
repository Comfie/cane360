using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Web.Services;

public sealed class DatabaseHealthCheck(ApplicationDbContext context) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        context.Database.CanConnectAsync(cancellationToken);
}
