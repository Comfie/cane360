using System.Data;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Finance;
using Cane360.Domain.Auditing;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class FinanceRepository(ApplicationDbContext context) : IFinanceRepository
{
    public async Task<IFinanceTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken) =>
        new FinanceTransaction(await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken));

    public async Task<IReadOnlyList<OperationalTransaction>> GetTransactionsAsync(Guid tenantId,
        Guid farmId, DateOnly? from, DateOnly? to, string? type, string? category, string? status, string? search,
        CancellationToken cancellationToken)
    {
        IQueryable<OperationalTransaction> query = context.OperationalTransactions.AsNoTracking()
            .Include(x => x.Allocations).Where(x => x.TenantId == tenantId && x.FarmId == farmId);
        if (from.HasValue) query = query.Where(x => x.EventDate >= from.Value);
        if (to.HasValue) query = query.Where(x => x.EventDate <= to.Value);
        if (Enum.TryParse(type, true, out OperationalTransactionType parsedType))
            query = query.Where(x => x.Type == parsedType);
        if (Enum.TryParse(category, true, out OperationalFinanceCategory parsedCategory))
            query = query.Where(x => x.Category == parsedCategory);
        if (Enum.TryParse(status, true, out OperationalTransactionStatus parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.PayeeOrPayer, $"%{term}%") ||
                (x.SourceReference != null && EF.Functions.ILike(x.SourceReference, $"%{term}%")));
        }
        return await query.OrderByDescending(x => x.EventDate).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<OperationalTransaction?> GetTransactionAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<OperationalTransaction> query = context.OperationalTransactions
            .Include(x => x.Allocations).Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.Id == id);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<OperationalTransaction?> GetTransactionByPostingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.OperationalTransactions
        .AsNoTracking().Include(x => x.Allocations).SingleOrDefaultAsync(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.PostedIdempotencyKey == idempotencyKey &&
            x.ReversalOfOperationalTransactionId == null, cancellationToken);

    public Task<OperationalTransaction?> GetReversalByPostingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.OperationalTransactions
        .AsNoTracking().Include(x => x.Allocations).SingleOrDefaultAsync(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.PostedIdempotencyKey == idempotencyKey &&
            x.ReversalOfOperationalTransactionId != null, cancellationToken);

    public async Task<IReadOnlyList<OperationalCostPosting>> GetCostPostingsAsync(Guid tenantId,
        Guid farmId, Guid cropCycleId, CancellationToken cancellationToken) => await context
        .OperationalCostPostings.AsNoTracking().Where(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.CropCycleId == cropCycleId).OrderBy(x => x.Id)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CostSourceChain>> GetCostSourceChainsAsync(Guid tenantId,
        Guid farmId, IReadOnlyCollection<Guid> postingIds, CancellationToken cancellationToken)
    {
        var sources = await context.OperationalCostPostings.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId && postingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PayrollEarningLineId, x.TransactionAllocationId })
            .ToListAsync(cancellationToken);
        Guid[] earningIds = sources.Where(x => x.PayrollEarningLineId.HasValue)
            .Select(x => x.PayrollEarningLineId!.Value).Distinct().ToArray();
        var payrollChains = await (from earning in context.PayrollEarningLines.AsNoTracking()
            join calculation in context.PayrollCalculations.AsNoTracking()
                on new { earning.PayrollCalculationId, earning.TenantId, earning.FarmId }
                equals new { PayrollCalculationId = calculation.Id, calculation.TenantId, calculation.FarmId }
            where earningIds.Contains(earning.Id) && earning.TenantId == tenantId && earning.FarmId == farmId
            select new { EarningId = earning.Id, calculation.PayrollRunId,
                PayrollCalculationId = calculation.Id, calculation.CalculationVersion,
                earning.PayrollWorkerLineId, earning.WorkerProfileId, WorkRecordId = earning.EvidenceId })
            .ToDictionaryAsync(x => x.EarningId, cancellationToken);
        Guid[] allocationIds = sources.Where(x => x.TransactionAllocationId.HasValue)
            .Select(x => x.TransactionAllocationId!.Value).Distinct().ToArray();
        var transactionChains = await context.TransactionAllocations.AsNoTracking()
            .Where(x => allocationIds.Contains(x.Id) && x.TenantId == tenantId && x.FarmId == farmId)
            .ToDictionaryAsync(x => x.Id, x => x.OperationalTransactionId, cancellationToken);

        return sources.Select(source =>
        {
            payrollChains.TryGetValue(source.PayrollEarningLineId ?? Guid.Empty, out var payroll);
            transactionChains.TryGetValue(source.TransactionAllocationId ?? Guid.Empty, out Guid transactionId);
            return new CostSourceChain(source.Id, payroll?.PayrollRunId,
                payroll?.PayrollCalculationId, payroll?.CalculationVersion,
                payroll?.PayrollWorkerLineId, payroll?.WorkerProfileId, payroll?.WorkRecordId,
                transactionId == Guid.Empty ? null : transactionId, source.TransactionAllocationId);
        }).ToArray();
    }

    public async Task<IReadOnlyList<OperationalCostPosting>> GetCostPostingsForAllocationsAsync(
        Guid tenantId, Guid farmId, IReadOnlyCollection<Guid> allocationIds,
        CancellationToken cancellationToken) => await context.OperationalCostPostings.AsNoTracking()
        .Where(x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.TransactionAllocationId.HasValue && allocationIds.Contains(x.TransactionAllocationId.Value))
        .ToListAsync(cancellationToken);

    public Task<bool> HasPayrollCostPostingAsync(Guid tenantId, Guid farmId, Guid earningLineId,
        Guid cropCycleId, CancellationToken cancellationToken) => context.OperationalCostPostings.AnyAsync(
            x => x.TenantId == tenantId && x.FarmId == farmId &&
            x.PayrollEarningLineId == earningLineId && x.CropCycleId == cropCycleId &&
            x.ReversalOfOperationalCostPostingId == null, cancellationToken);

    public void RemoveDraftAllocations(IReadOnlyCollection<TransactionAllocation> allocations) =>
        context.TransactionAllocations.RemoveRange(allocations);
    public void Add(OperationalTransaction transaction) => context.OperationalTransactions.Add(transaction);
    public void Add(OperationalCostPosting posting) => context.OperationalCostPostings.Add(posting);
    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
    public void Add(FinanceAuditEventLink auditLink) => context.FinanceAuditEventLinks.Add(auditLink);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { return await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        { throw new ConflictException("This finance record changed before the action completed. Refresh and try again."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres &&
            (postgres.SqlState == PostgresErrorCodes.UniqueViolation ||
             postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
             postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation ||
             postgres.SqlState == PostgresErrorCodes.CheckViolation))
        { throw new ConflictException("The finance operation conflicts with an authoritative posting, scope, or idempotency rule."); }
    }

    private sealed class FinanceTransaction(IDbContextTransaction transaction) : IFinanceTransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try { await transaction.CommitAsync(cancellationToken); }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
            { throw new ConflictException("The finance operation raced with another authoritative operation. Refresh and retry."); }
        }
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
