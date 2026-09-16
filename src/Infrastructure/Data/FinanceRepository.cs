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
        return await TransactionQuery(tenantId, farmId, from, to, type, category, status, search)
            .Include(x => x.Allocations).OrderByDescending(x => x.EventDate)
            .ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public async Task<FinanceTransactionPageSource> GetTransactionPageAsync(Guid tenantId,
        Guid farmId, DateOnly? from, DateOnly? to, string? type, string? category, string? status,
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ArgumentOutOfRangeException(nameof(page));
        IQueryable<OperationalTransaction> query = TransactionQuery(tenantId, farmId, from, to,
            type, category, status, search);
        var rows = await query
            .OrderByDescending(x => x.EventDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(transaction => new
            {
                Transaction = transaction,
                Allocations = transaction.Allocations.OrderBy(x => x.Id).ToArray(),
                Total = query.Count(),
                Expenses = query.Where(x => x.Status == OperationalTransactionStatus.Posted &&
                    x.Type == OperationalTransactionType.Expense).Sum(x => (decimal?)x.AmountUsd) ?? 0,
                Income = query.Where(x => x.Status == OperationalTransactionStatus.Posted &&
                    x.Type == OperationalTransactionType.Income).Sum(x => (decimal?)x.AmountUsd) ?? 0,
                Drafts = query.Count(x => x.Status == OperationalTransactionStatus.Draft)
            }).ToArrayAsync(cancellationToken);
        if (rows.Length > 0)
            return new(rows.Select(x => Map(x.Transaction, x.Allocations)).ToArray(), rows[0].Total,
                rows[0].Expenses, rows[0].Income, rows[0].Drafts);
        if (page == 1) return new([], 0, 0, 0, 0);
        var summary = await query.GroupBy(_ => 1).Select(group => new
        {
            Total = group.Count(),
            Expenses = group.Sum(x => x.Status == OperationalTransactionStatus.Posted &&
                x.Type == OperationalTransactionType.Expense ? x.AmountUsd : 0),
            Income = group.Sum(x => x.Status == OperationalTransactionStatus.Posted &&
                x.Type == OperationalTransactionType.Income ? x.AmountUsd : 0),
            Drafts = group.Count(x => x.Status == OperationalTransactionStatus.Draft)
        }).SingleOrDefaultAsync(cancellationToken);
        return new([], summary?.Total ?? 0, summary?.Expenses ?? 0,
            summary?.Income ?? 0, summary?.Drafts ?? 0);
    }

    private static OperationalTransactionDto Map(OperationalTransaction transaction,
        IReadOnlyCollection<TransactionAllocation> allocations) => new(transaction.Id,
        transaction.Type.ToString(), transaction.Category.ToString(),
        transaction.EventDate.ToString("yyyy-MM-dd"), transaction.PayeeOrPayer,
        transaction.AmountUsd, transaction.SourceReference, transaction.Notes,
        transaction.Status.ToString(), transaction.Version, transaction.CreatedAt,
        transaction.PostedAt, transaction.IsClosedCycleCorrection,
        transaction.ClosedCycleCorrectionReason, transaction.ReversalOfOperationalTransactionId,
        transaction.ReversalReason, allocations.Select(x => new TransactionAllocationDto(x.Id,
            x.CropCycleId, x.FieldId, x.Category.ToString(), x.AmountUsd,
            x.AllocationType.ToString())).ToArray());

    private IQueryable<OperationalTransaction> TransactionQuery(Guid tenantId, Guid farmId,
        DateOnly? from, DateOnly? to, string? type, string? category, string? status, string? search)
    {
        IQueryable<OperationalTransaction> query = context.OperationalTransactions.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId);
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
        return query;
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

    public async Task<IReadOnlyList<Budget>> GetBudgetsAsync(Guid tenantId, Guid farmId,
        Guid cropCycleId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Budget> query = context.Budgets.Include(x => x.Lines).Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.CropCycleId == cropCycleId);
        if (!trackChanges) query = query.AsNoTracking();
        return await query.OrderByDescending(x => x.Version).ToListAsync(cancellationToken);
    }

    public Task<Budget?> GetBudgetAsync(Guid tenantId, Guid farmId, Guid budgetId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Budget> query = context.Budgets.Include(x => x.Lines).Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.Id == budgetId);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Budget?> GetCurrentApprovedBudgetAsync(Guid tenantId, Guid farmId,
        Guid cropCycleId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Budget> query = context.Budgets.Include(x => x.Lines).Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.CropCycleId == cropCycleId &&
            x.Status == BudgetStatus.Approved);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Budget?> GetBudgetByApprovalKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken) => context.Budgets.AsNoTracking()
        .Include(x => x.Lines).SingleOrDefaultAsync(x => x.TenantId == tenantId &&
            x.FarmId == farmId && x.ApprovalIdempotencyKey == idempotencyKey, cancellationToken);

    public void RemoveDraftAllocations(IReadOnlyCollection<TransactionAllocation> allocations) =>
        context.TransactionAllocations.RemoveRange(allocations);
    public void Add(OperationalTransaction transaction) => context.OperationalTransactions.Add(transaction);
    public void Add(OperationalCostPosting posting) => context.OperationalCostPostings.Add(posting);
    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
    public void Add(FinanceAuditEventLink auditLink) => context.FinanceAuditEventLinks.Add(auditLink);
    public void Add(Budget budget) => context.Budgets.Add(budget);
    public void Remove(BudgetLine line) => context.BudgetLines.Remove(line);

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
