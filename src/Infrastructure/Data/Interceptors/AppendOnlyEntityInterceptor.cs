using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cane360.Infrastructure.Data.Interceptors;

public sealed class AppendOnlyEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        RejectMutations(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        RejectMutations(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void RejectMutations(DbContext? context)
    {
        if (context is null) return;
        foreach (var entry in context.ChangeTracker.Entries().Where(entry =>
            (entry.Entity is AuditEvent or StockMovement or ApprovalDecision or CorrectionRecord or InventoryAuditEventLink) &&
            entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} records are append-only.");
        }
    }
}
