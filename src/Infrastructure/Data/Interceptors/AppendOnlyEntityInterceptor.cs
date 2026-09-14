using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Domain.Finance;
using Cane360.Domain.MillRecords;
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
            (entry.Entity is AuditEvent or StockMovement or ApprovalDecision or CorrectionRecord or InventoryAuditEventLink or PayrollAuditEventLink or FinanceAuditEventLink or OperationalCostPosting or AdvanceApproval or AdvanceIssue or PayrollCalculation or PayrollWorkerLine or PayrollEarningLine or PayrollAdvanceDeduction or PayrollApproval or PayrollEvidenceConsumption or AdvanceRecovery or PayrollPayment or PaymentAcknowledgement or PayrollPaymentReversal or PayrollSettlementClosure or PayrollSettlementReopen or StatementTicketMatch or EvidenceDocument or MillRecordAuditEventLink or MillRecordExport) &&
            entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} records are append-only.");
        }
        foreach (var entry in context.ChangeTracker.Entries<WeighbridgeTicket>().Where(entry =>
            entry.State is EntityState.Modified or EntityState.Deleted &&
            entry.Property(x => x.Status).OriginalValue == WeighbridgeTicketStatus.Recorded))
        {
            throw new InvalidOperationException("Recorded weighbridge tickets are immutable.");
        }
        foreach (var entry in context.ChangeTracker.Entries<GrowerStatement>().Where(entry =>
            entry.State is EntityState.Modified or EntityState.Deleted &&
            entry.Property(x => x.Status).OriginalValue == GrowerStatementStatus.Recorded))
        {
            throw new InvalidOperationException("Recorded grower statements are immutable.");
        }
        if (context.ChangeTracker.Entries<Mill>().Any(entry => entry.State == EntityState.Deleted))
            throw new InvalidOperationException("Mill references cannot be hard deleted.");
        foreach (var entry in context.ChangeTracker.Entries<WorkRecord>().Where(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            if (context.Set<PayrollEvidenceConsumption>().Any(x => x.EvidenceId == entry.Entity.Id)) throw new InvalidOperationException("Labour evidence consumed by an approved payroll is locked.");
        }
    }
}
