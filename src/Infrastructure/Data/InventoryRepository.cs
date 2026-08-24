using System.Data;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class InventoryRepository(ApplicationDbContext context) : IInventoryRepository
{
    public async Task<IReadOnlyList<UnitOfMeasure>> GetUnitsAsync(
        Guid tenantId, bool trackChanges, CancellationToken cancellationToken) =>
        await Track(context.UnitOfMeasures.Where(entity => entity.TenantId == tenantId), trackChanges)
            .OrderBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<UnitOfMeasure?> GetUnitAsync(
        Guid tenantId, Guid unitId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.UnitOfMeasures.Where(entity => entity.TenantId == tenantId && entity.Id == unitId), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryItem>> GetItemsAsync(
        Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) =>
        await Track(context.InventoryItems.Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId), trackChanges)
            .OrderBy(entity => entity.Status).ThenBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<InventoryItem?> GetItemAsync(
        Guid tenantId, Guid farmId, Guid itemId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.InventoryItems.Where(entity =>
            entity.TenantId == tenantId && entity.FarmId == farmId && entity.Id == itemId), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(
        Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) =>
        await Track(context.Suppliers.Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId), trackChanges)
            .OrderBy(entity => entity.Status).ThenBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<Supplier?> GetSupplierAsync(
        Guid tenantId, Guid farmId, Guid supplierId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.Suppliers.Where(entity =>
            entity.TenantId == tenantId && entity.FarmId == farmId && entity.Id == supplierId), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryLot>> GetLotsAsync(
        Guid tenantId, Guid farmId, Guid? itemId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.InventoryLots.Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId);
        if (itemId.HasValue) query = query.Where(entity => entity.InventoryItemId == itemId);
        return await Track(query, trackChanges).OrderBy(entity => entity.Code).ToListAsync(cancellationToken);
    }

    public Task<InventoryLot?> GetLotAsync(
        Guid tenantId, Guid farmId, Guid lotId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.InventoryLots.Where(entity =>
            entity.TenantId == tenantId && entity.FarmId == farmId && entity.Id == lotId), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<StockReceipt>> GetReceiptsAsync(
        Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) =>
        await Track(context.StockReceipts.Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId), trackChanges)
            .Include(entity => entity.Lines)
            .OrderByDescending(entity => entity.ReceiptDate)
            .ThenByDescending(entity => entity.Created)
            .ToListAsync(cancellationToken);

    public Task<StockReceipt?> GetReceiptAsync(
        Guid tenantId, Guid farmId, Guid receiptId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.StockReceipts.Where(entity =>
                entity.TenantId == tenantId && entity.FarmId == farmId && entity.Id == receiptId), trackChanges)
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        Guid tenantId, Guid farmId, Guid? itemId, CancellationToken cancellationToken)
    {
        var query = context.StockMovements.AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId);
        if (itemId.HasValue) query = query.Where(entity => entity.InventoryItemId == itemId);
        return await query.OrderByDescending(entity => entity.PostingSequence).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(StockPosition Position, StockLedgerSnapshot Snapshot)>> GetStockOnHandAsync(
        Guid tenantId, Guid farmId, CancellationToken cancellationToken)
    {
        var positions = await context.StockPositions.AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId)
            .OrderBy(entity => entity.InventoryItemId).ThenBy(entity => entity.PositionKey)
            .ToListAsync(cancellationToken);
        var sums = await context.StockMovements.AsNoTracking()
            .Where(entity => entity.TenantId == tenantId && entity.FarmId == farmId)
            .GroupBy(entity => entity.StockPositionId)
            .Select(group => new
            {
                PositionId = group.Key,
                Quantity = group.Sum(entity => entity.SignedQuantity),
                ValueUsd = group.Sum(entity => entity.SignedValueUsd)
            })
            .ToDictionaryAsync(entity => entity.PositionId, cancellationToken);
        return positions.Select(position =>
        {
            var snapshot = sums.GetValueOrDefault(position.Id);
            return (
                position,
                new StockLedgerSnapshot(snapshot?.Quantity ?? 0, snapshot?.ValueUsd ?? 0));
        }).ToArray();
    }

    public Task<ApprovalDecision?> GetOpeningApprovalAsync(
        Guid receiptId, long subjectVersion, CancellationToken cancellationToken) =>
        context.ApprovalDecisions.AsNoTracking().SingleOrDefaultAsync(entity =>
            entity.StockReceiptId == receiptId && entity.SubjectVersion == subjectVersion &&
            entity.Outcome == ApprovalOutcome.Approved, cancellationToken);

    public Task<StockPosition?> GetPositionAsync(
        Guid tenantId,
        Guid farmId,
        Guid storeId,
        Guid itemId,
        Guid? lotId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var positionKey = lotId?.ToString("N") ?? "UNBATCHED";
        return Track(context.StockPositions.Where(entity =>
            entity.TenantId == tenantId && entity.FarmId == farmId && entity.StoreId == storeId &&
            entity.InventoryItemId == itemId && entity.PositionKey == positionKey), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<StockLedgerSnapshot> GetPositionSnapshotAsync(
        Guid positionId, CancellationToken cancellationToken)
    {
        var values = await context.StockMovements.AsNoTracking()
            .Where(entity => entity.StockPositionId == positionId)
            .GroupBy(_ => 1)
            .Select(group => new StockLedgerSnapshot(
                group.Sum(entity => entity.SignedQuantity),
                group.Sum(entity => entity.SignedValueUsd)))
            .SingleOrDefaultAsync(cancellationToken);
        return values ?? new StockLedgerSnapshot(0, 0);
    }

    public async Task<IReadOnlyList<StockMovement>> GetReceiptMovementsAsync(
        Guid receiptId, CancellationToken cancellationToken) =>
        await context.StockMovements.AsNoTracking()
            .Where(movement => !movement.ReversalOfStockMovementId.HasValue &&
                context.StockReceiptLines.Any(line =>
                    line.StockReceiptId == receiptId && line.Id == movement.StockReceiptLineId))
            .OrderBy(movement => movement.PostingSequence)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasLaterPositionMovementsAsync(
        IReadOnlyCollection<StockMovement> originals, CancellationToken cancellationToken)
    {
        foreach (var group in originals.GroupBy(movement => movement.StockPositionId))
        {
            var maximumOriginalSequence = group.Max(movement => movement.PostingSequence);
            var originalIds = group.Select(movement => movement.Id).ToArray();
            if (await context.StockMovements.AsNoTracking().AnyAsync(movement =>
                movement.StockPositionId == group.Key &&
                movement.PostingSequence > maximumOriginalSequence &&
                (!movement.ReversalOfStockMovementId.HasValue ||
                 !originalIds.Contains(movement.ReversalOfStockMovementId.Value)), cancellationToken))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<IReadOnlyList<InventoryApplicationRule>> GetRulesAsync(
        Guid tenantId, Guid farmId, CancellationToken cancellationToken) =>
        await context.InventoryApplicationRules.AsNoTracking()
            .Where(rule => rule.TenantId == tenantId && rule.FarmId == farmId)
            .OrderBy(rule => rule.InventoryItemId).ThenByDescending(rule => rule.EffectiveFrom)
            .ToListAsync(cancellationToken);

    public Task<InventoryApplicationRule?> GetEffectiveRuleAsync(
        Guid tenantId, Guid farmId, Guid itemId, Guid activityTypeId, DateOnly date,
        CancellationToken cancellationToken) => context.InventoryApplicationRules.AsNoTracking()
        .SingleOrDefaultAsync(rule => rule.TenantId == tenantId && rule.FarmId == farmId &&
            rule.InventoryItemId == itemId && rule.ActivityTypeId == activityTypeId &&
            rule.EffectiveFrom <= date && (rule.EffectiveTo == null || rule.EffectiveTo >= date), cancellationToken);

    public async Task<(decimal Quantity, decimal ValueUsd)> GetItemStockSnapshotAsync(
        Guid tenantId, Guid farmId, Guid itemId, CancellationToken cancellationToken)
    {
        var snapshot = await context.StockMovements.AsNoTracking()
            .Where(movement => movement.TenantId == tenantId && movement.FarmId == farmId &&
                movement.InventoryItemId == itemId)
            .GroupBy(_ => 1)
            .Select(group => new { Quantity = group.Sum(x => x.SignedQuantity), Value = group.Sum(x => x.SignedValueUsd) })
            .SingleOrDefaultAsync(cancellationToken);
        return snapshot is null ? (0, 0) : (snapshot.Quantity, snapshot.Value);
    }

    public async Task<IReadOnlyList<InputRequest>> GetInputRequestsAsync(
        Guid tenantId, Guid farmId, Guid? activityId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.InputRequests.Where(request => request.TenantId == tenantId && request.FarmId == farmId);
        if (activityId.HasValue) query = query.Where(request => request.ActivityId == activityId);
        return await Track(query.Include(request => request.Lines), trackChanges)
            .OrderByDescending(request => request.Created).ToListAsync(cancellationToken);
    }

    public Task<InputRequest?> GetInputRequestAsync(
        Guid tenantId, Guid farmId, Guid requestId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.InputRequests.Where(request => request.TenantId == tenantId && request.FarmId == farmId && request.Id == requestId)
            .Include(request => request.Lines), trackChanges).SingleOrDefaultAsync(cancellationToken);

    public Task<ApprovalDecision?> GetInputRequestApprovalAsync(
        Guid requestId, long subjectVersion, CancellationToken cancellationToken) =>
        context.ApprovalDecisions.AsNoTracking().SingleOrDefaultAsync(decision =>
            decision.InputRequestId == requestId && decision.SubjectVersion == subjectVersion, cancellationToken);

    public async Task<IReadOnlyList<StockIssue>> GetStockIssuesAsync(
        Guid tenantId, Guid farmId, Guid? requestId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.StockIssues.Where(issue => issue.TenantId == tenantId && issue.FarmId == farmId);
        if (requestId.HasValue) query = query.Where(issue => issue.InputRequestId == requestId);
        return await Track(query.Include(issue => issue.Lines), trackChanges)
            .OrderByDescending(issue => issue.Created).ToListAsync(cancellationToken);
    }

    public Task<StockIssue?> GetStockIssueAsync(
        Guid tenantId, Guid farmId, Guid issueId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.StockIssues.Where(issue => issue.TenantId == tenantId && issue.FarmId == farmId && issue.Id == issueId)
            .Include(issue => issue.Lines), trackChanges).SingleOrDefaultAsync(cancellationToken);

    public async Task<decimal> GetPostedIssueQuantityAsync(Guid requestLineId, CancellationToken cancellationToken) =>
        -await context.StockMovements.AsNoTracking()
            .Where(movement => movement.StockIssueLineId.HasValue &&
                context.StockIssueLines.Any(line => line.Id == movement.StockIssueLineId && line.InputRequestLineId == requestLineId))
            .SumAsync(movement => movement.SignedQuantity, cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> GetIssueMovementsAsync(Guid issueId, CancellationToken cancellationToken) =>
        await context.StockMovements.AsNoTracking()
            .Where(movement => movement.StockIssueLineId.HasValue && !movement.ReversalOfStockMovementId.HasValue &&
                context.StockIssueLines.Any(line => line.StockIssueId == issueId && line.Id == movement.StockIssueLineId))
            .OrderBy(movement => movement.PostingSequence).ToListAsync(cancellationToken);

    public Task<bool> HasDependentFieldAccountabilityAsync(Guid issueId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<ManagerInvitation?> GetManagerInvitationByHashAsync(
        string tokenHash, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.ManagerInvitations.Where(invitation => invitation.TokenHash == tokenHash), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ManagerInvitation>> GetManagerInvitationsAsync(
        Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken) =>
        await Track(context.ManagerInvitations.Where(invitation => invitation.TenantId == tenantId && invitation.FarmId == farmId), trackChanges)
            .OrderByDescending(invitation => invitation.Created).ToListAsync(cancellationToken);

    public async Task<IInventoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken) =>
        new InventoryTransaction(await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken));

    public void ResetTrackedChanges() => context.ChangeTracker.Clear();

    public async Task LockStoreAsync(
        Guid tenantId, Guid farmId, Guid storeId, CancellationToken cancellationToken)
    {
        var locked = await context.Stores.FromSqlInterpolated(
                $"SELECT store.* FROM farm.\"Stores\" AS store INNER JOIN farm.\"Farms\" AS farm ON farm.\"Id\" = store.\"FarmId\" WHERE store.\"Id\" = {storeId} AND store.\"FarmId\" = {farmId} AND farm.\"TenantId\" = {tenantId} FOR UPDATE OF store")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (locked is null) throw new NotFoundException(storeId.ToString(), "Store");
    }

    public async Task LockReceiptSourceAsync(
        Guid tenantId, Guid farmId, Guid receiptId, CancellationToken cancellationToken)
    {
        var locked = await context.StockReceipts.FromSqlInterpolated(
                $"SELECT * FROM inventory.\"StockReceipts\" WHERE \"Id\" = {receiptId} AND \"TenantId\" = {tenantId} AND \"FarmId\" = {farmId} FOR UPDATE")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (locked is null) throw new NotFoundException(receiptId.ToString(), "Stock receipt");
    }

    public async Task LockStockPositionsAsync(
        IReadOnlyCollection<Guid> positionIds, CancellationToken cancellationToken)
    {
        foreach (var positionId in positionIds.Order())
        {
            var locked = await context.StockPositions.FromSqlInterpolated(
                    $"SELECT * FROM inventory.\"StockPositions\" WHERE \"Id\" = {positionId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (locked is null) throw new NotFoundException(positionId.ToString(), "Stock position");
        }
    }

    public async Task LockInputRequestLinesAsync(
        IReadOnlyCollection<Guid> requestLineIds, CancellationToken cancellationToken)
    {
        foreach (var requestLineId in requestLineIds.Order())
        {
            var locked = await context.InputRequestLines.FromSqlInterpolated(
                    $"SELECT * FROM inventory.\"InputRequestLines\" WHERE \"Id\" = {requestLineId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (locked is null) throw new NotFoundException(requestLineId.ToString(), "Input request line");
        }
    }

    public async Task LockStockIssueAsync(
        Guid tenantId, Guid farmId, Guid issueId, CancellationToken cancellationToken)
    {
        var locked = await context.StockIssues.FromSqlInterpolated(
                $"SELECT * FROM inventory.\"StockIssues\" WHERE \"Id\" = {issueId} AND \"TenantId\" = {tenantId} AND \"FarmId\" = {farmId} FOR UPDATE")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (locked is null) throw new NotFoundException(issueId.ToString(), "Stock issue");
    }

    public void Add(UnitOfMeasure unit) => context.UnitOfMeasures.Add(unit);
    public void Add(InventoryItem item) => context.InventoryItems.Add(item);
    public void Add(Supplier supplier) => context.Suppliers.Add(supplier);
    public void Add(InventoryLot lot) => context.InventoryLots.Add(lot);
    public void Add(StockReceipt receipt) => context.StockReceipts.Add(receipt);
    public void Add(StockPosition position) => context.StockPositions.Add(position);
    public void Add(StockMovement movement) => context.StockMovements.Add(movement);
    public void Add(ApprovalDecision approval) => context.ApprovalDecisions.Add(approval);
    public void Add(CorrectionRecord correction) => context.CorrectionRecords.Add(correction);
    public void Add(InventoryAuditEventLink auditLink) => context.InventoryAuditEventLinks.Add(auditLink);
    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
    public void Add(InventoryApplicationRule rule) => context.InventoryApplicationRules.Add(rule);
    public void Add(InputRequest request) => context.InputRequests.Add(request);
    public void Add(StockIssue issue) => context.StockIssues.Add(issue);
    public void Add(ManagerInvitation invitation) => context.ManagerInvitations.Add(invitation);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This inventory record changed before the action could be completed. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres)
        {
            if (postgres.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                throw new InventorySerializationFailureException(
                    "The serializable inventory posting must be retried.", exception);
            }
            var conflict = postgres.ConstraintName switch
            {
                "AK_UnitOfMeasures_TenantId_Code" or "IX_UnitOfMeasures_TenantId_Code" => "This unit code already exists in the tenant.",
                "IX_InventoryItems_FarmId_Code" => "This inventory item code already exists on the farm.",
                "IX_Suppliers_FarmId_Code" => "This supplier code already exists on the farm.",
                "IX_InventoryLots_InventoryItemId_Code" => "This lot code already exists for the item.",
                "IX_StockMovements_PostingIdentity" => "This stock posting has already been recorded.",
                "EX_InventoryApplicationRules_NoOverlap" => "An application rule already covers part of this effective period for the item and activity type.",
                "IX_InputRequests_SubmissionIdempotencyKey" => "This request submission has already been recorded.",
                "IX_StockIssues_PostingIdempotencyKey" => "This stock issue posting has already been recorded.",
                "IX_StockIssues_ReversalIdempotencyKey" => "This stock issue reversal has already been recorded.",
                "IX_ManagerInvitations_TokenHash" => "This manager invitation token already exists.",
                "IX_ManagerInvitations_TenantId_PersonId" => "An active invitation already exists for this farm manager.",
                _ => null
            };
            if (conflict is not null) throw new ConflictException(conflict);
            throw;
        }
        catch (Exception exception) when (IsSerializationFailure(exception))
        {
            throw new InventorySerializationFailureException(
                "The serializable inventory posting must be retried.", exception);
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }
        }
        return false;
    }

    private static IQueryable<T> Track<T>(IQueryable<T> query, bool trackChanges) where T : class =>
        trackChanges ? query : query.AsNoTracking();

    private sealed class InventoryTransaction(IDbContextTransaction transaction) : IInventoryTransaction
    {
        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                throw new InventorySerializationFailureException(
                    "The serializable inventory posting must be retried.", exception);
            }
        }
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
