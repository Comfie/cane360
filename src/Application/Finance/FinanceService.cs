using Cane360.Domain.Auditing;

namespace Cane360.Application.Finance;

public sealed class FinanceService(IFarmSetupRepository farms, IFinanceRepository finance,
    IPayrollCostProjectionService payrollProjection, IUser user, TimeProvider clock) : IFinanceService
{
    public async Task<IReadOnlyList<OperationalTransactionDto>> GetTransactionsAsync(
        FinanceTransactionFilter filter, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        return (await finance.GetTransactionsAsync(context.Tenant.Id, context.Farm.Id, filter.From,
            filter.To, filter.Type, filter.Category, filter.Status, filter.Search, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<OperationalTransactionDto> GetTransactionAsync(Guid transactionId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        return Map(await RequireTransactionAsync(context, transactionId, false, cancellationToken));
    }

    public async Task<OperationalTransactionDto> CreateAsync(CreateOperationalTransactionInput input,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireOperator(context);
        var transaction = Apply(() => OperationalTransaction.Create(context.Tenant.Id, context.Farm.Id,
            ParseType(input.Type), ParseCategory(input.Category), input.EventDate, input.PayeeOrPayer,
            input.AmountUsd, input.SourceReference, input.Notes, context.UserId, clock.GetUtcNow(),
            Correlation()), nameof(input.AmountUsd));
        finance.Add(transaction);
        Audit(context, transaction.Id, "TransactionCreated", null,
            $"{transaction.Type} operational transaction draft created.",
            audit => FinanceAuditEventLink.ForTransaction(audit.Id, context.Tenant.Id, context.Farm.Id, transaction.Id));
        await finance.SaveChangesAsync(cancellationToken);
        return Map(transaction);
    }

    public async Task<OperationalTransactionDto> UpdateAsync(Guid transactionId,
        UpdateOperationalTransactionInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        OperationalTransaction transaction = await RequireTransactionAsync(context, transactionId,
            true, cancellationToken);
        Apply(() => transaction.Update(ParseType(input.Type), ParseCategory(input.Category),
            input.EventDate, input.PayeeOrPayer, input.AmountUsd, input.SourceReference, input.Notes,
            input.ExpectedVersion), nameof(input.ExpectedVersion));
        Audit(context, transaction.Id, "DraftChanged", null,
            "Operational transaction draft changed before posting.",
            audit => FinanceAuditEventLink.ForTransaction(audit.Id, context.Tenant.Id, context.Farm.Id, transaction.Id));
        await finance.SaveChangesAsync(cancellationToken);
        return Map(transaction);
    }

    public async Task<OperationalTransactionDto> SetAllocationsAsync(Guid transactionId,
        SetTransactionAllocationsInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        OperationalTransaction transaction = await RequireTransactionAsync(context, transactionId,
            true, cancellationToken);
        var allocations = input.Allocations.Select(item =>
        {
            TransactionAllocationType type = ParseAllocationType(item.AllocationType);
            ValidateScope(context.Farm, item.FieldId, item.CropCycleId, false, false, null, context);
            return Apply(() => TransactionAllocation.Create(context.Tenant.Id, context.Farm.Id,
                transaction.Id, item.CropCycleId, item.FieldId, ParseCategory(item.Category),
                item.AmountUsd, type, clock.GetUtcNow()), nameof(input.Allocations));
        }).ToArray();
        TransactionAllocation[] existing = transaction.Allocations.ToArray();
        Apply(() => transaction.ReplaceAllocations(allocations, input.ExpectedVersion),
            nameof(input.ExpectedVersion));
        finance.RemoveDraftAllocations(existing);
        Audit(context, transaction.Id, "AllocationsFinalized", null,
            "Draft allocations were replaced and reconcile exactly to the transaction amount.",
            audit => FinanceAuditEventLink.ForTransaction(audit.Id, context.Tenant.Id, context.Farm.Id, transaction.Id));
        await finance.SaveChangesAsync(cancellationToken);
        return Map(transaction);
    }

    public async Task<OperationalTransactionDto> PostAsync(Guid transactionId,
        PostOperationalTransactionInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(true, cancellationToken);
        RequireOperator(context);
        await using IFinanceTransaction databaseTransaction = await finance.BeginSerializableTransactionAsync(cancellationToken);
        OperationalTransaction? byKey = await finance.GetTransactionByPostingKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (byKey is not null)
        {
            if (byKey.Id != transactionId) throw new ConflictException(
                "This posting idempotency key is bound to another transaction.");
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(byKey);
        }
        OperationalTransaction transaction = await RequireTransactionAsync(context, transactionId,
            true, cancellationToken);
        foreach (TransactionAllocation allocation in transaction.Allocations)
            ValidateScope(context.Farm, allocation.FieldId, allocation.CropCycleId, true,
                input.AuthorizedClosedCycleCorrection, input.CorrectionReason, context);
        Apply(() => transaction.Post(context.UserId, clock.GetUtcNow(), input.IdempotencyKey,
            input.ExpectedVersion, input.AuthorizedClosedCycleCorrection, input.CorrectionReason),
            nameof(input.ExpectedVersion));
        if (transaction.Type == OperationalTransactionType.Expense)
        {
            foreach (TransactionAllocation allocation in transaction.Allocations.Where(x =>
                x.AllocationType == TransactionAllocationType.CropCycleDirect))
            {
                var posting = OperationalCostPosting.ForDirectExpense(context.Tenant.Id,
                    context.Farm.Id, allocation.FieldId!.Value, allocation.CropCycleId!.Value,
                    allocation, $"finance:{transaction.Id:N}:allocation:{allocation.Id:N}:posted");
                finance.Add(posting);
                Audit(context, posting.Id, "DirectExpenseCostPosted", input.CorrectionReason,
                    "Posted expense allocation projected as direct crop-cycle cost.",
                    audit => FinanceAuditEventLink.ForCostPosting(audit.Id, context.Tenant.Id, context.Farm.Id, posting.Id));
            }
        }
        Audit(context, transaction.Id, "TransactionPosted", input.CorrectionReason,
            $"{transaction.Type} transaction posted with exactly reconciled allocations.",
            audit => FinanceAuditEventLink.ForTransaction(audit.Id, context.Tenant.Id, context.Farm.Id, transaction.Id));
        await finance.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        return Map(transaction);
    }

    public async Task<OperationalTransactionDto> ReverseAsync(Guid transactionId,
        ReverseOperationalTransactionInput input, CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireGrower(context);
        await using IFinanceTransaction databaseTransaction = await finance.BeginSerializableTransactionAsync(cancellationToken);
        OperationalTransaction? byKey = await finance.GetReversalByPostingKeyAsync(context.Tenant.Id,
            context.Farm.Id, input.IdempotencyKey, cancellationToken);
        if (byKey is not null)
        {
            if (byKey.ReversalOfOperationalTransactionId != transactionId)
                throw new ConflictException("This reversal idempotency key is bound to another transaction.");
            await databaseTransaction.CommitAsync(cancellationToken);
            return Map(byKey);
        }
        OperationalTransaction original = await RequireTransactionAsync(context, transactionId,
            false, cancellationToken);
        var reversal = Apply(() => OperationalTransaction.CreateReversal(original, input.Reason,
            context.UserId, clock.GetUtcNow(), input.IdempotencyKey, Correlation()), nameof(input.Reason));
        finance.Add(reversal);
        IReadOnlyList<OperationalCostPosting> postings = await finance.GetCostPostingsForAllocationsAsync(
            context.Tenant.Id, context.Farm.Id, original.Allocations.Select(x => x.Id).ToArray(), cancellationToken);
        foreach (OperationalCostPosting posting in postings.Where(x => x.ReversalOfOperationalCostPostingId == null))
        {
            var costReversal = OperationalCostPosting.Reverse(posting,
                $"finance:{reversal.Id:N}:cost:{posting.Id:N}:reversal");
            finance.Add(costReversal);
            Audit(context, costReversal.Id, "DirectExpenseCostReversed", input.Reason,
                "Direct-expense cost reversal appended from the original source posting.",
                audit => FinanceAuditEventLink.ForCostPosting(audit.Id, context.Tenant.Id, context.Farm.Id, costReversal.Id));
        }
        Audit(context, reversal.Id, "TransactionReversed", input.Reason,
            "Explicit operational transaction reversal appended; the original transaction remains intact.",
            audit => FinanceAuditEventLink.ForTransaction(audit.Id, context.Tenant.Id, context.Farm.Id, reversal.Id));
        await finance.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        return Map(reversal);
    }

    public async Task<CropCycleCostSummaryDto> GetCropCycleCostAsync(Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        Field? field = context.Farm.Fields.SingleOrDefault(x => x.CropCycles.Any(c => c.Id == cropCycleId));
        CropCycle cycle = field?.CropCycles.Single(x => x.Id == cropCycleId)
            ?? throw new NotFoundException(cropCycleId.ToString(), "Crop cycle");
        IReadOnlyList<OperationalCostPosting> postings = await finance.GetCostPostingsAsync(
            context.Tenant.Id, context.Farm.Id, cropCycleId, cancellationToken);
        decimal labour = Sum(postings, OperationalCostCategory.Labour);
        decimal inputs = Sum(postings, OperationalCostCategory.AppliedInput);
        decimal direct = Sum(postings, OperationalCostCategory.DirectExpense);
        decimal losses = Sum(postings, OperationalCostCategory.ApprovedInventoryLoss);
        decimal total = labour + inputs + direct + losses;
        decimal? hectares = null;
        try { hectares = field.ReportingHectares > 0 ? field.ReportingHectares : null; }
        catch (InvalidOperationException) { }
        decimal? tonnes = cycle.HarvestResult?.ActualTonnes is > 0 ? cycle.HarvestResult.ActualTonnes : null;
        IReadOnlyDictionary<Guid, CostSourceChain> chains = (await finance.GetCostSourceChainsAsync(
            context.Tenant.Id, context.Farm.Id, postings.Select(x => x.Id).ToArray(), cancellationToken))
            .ToDictionary(x => x.PostingId);
        return new(cycle.Id, field.Id, field.Name, labour, inputs, direct, losses, total, hectares,
            CropCostMath.PerUnit(total, hectares), tonnes, CropCostMath.PerUnit(total, tonnes),
            postings.Select(posting => MapSource(posting, chains.GetValueOrDefault(posting.Id))).ToArray());
    }

    public async Task<PayrollCostReconciliationDto> ReconcilePayrollAsync(
        CancellationToken cancellationToken)
    {
        Context context = await ContextAsync(false, cancellationToken);
        RequireGrower(context);
        await using IFinanceTransaction transaction = await finance.BeginSerializableTransactionAsync(cancellationToken);
        PayrollCostReconciliationDto result = await payrollProjection.ReconcileAsync(context.Tenant,
            context.Farm, user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<Context> ContextAsync(bool track, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        Tenant tenant = await farms.GetTenantForUserAsync(userId, track, cancellationToken)
            ?? throw new NotFoundException(userId, "Active grower or farm-manager membership");
        return new(tenant, tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(),
            "Active farm"), userId);
    }

    private Task<OperationalTransaction> RequireTransactionAsync(Context context, Guid id, bool track,
        CancellationToken cancellationToken) => RequireAsync(finance.GetTransactionAsync(context.Tenant.Id,
            context.Farm.Id, id, track, cancellationToken), id);

    private static async Task<OperationalTransaction> RequireAsync(Task<OperationalTransaction?> task, Guid id) =>
        await task ?? throw new NotFoundException(id.ToString(), "Operational transaction");

    private static void ValidateScope(Farm farm, Guid? fieldId, Guid? cycleId, bool posting,
        bool authorizedClosedCorrection, string? reason, Context context)
    {
        if (fieldId is null && cycleId is null) return;
        Field field = farm.Fields.SingleOrDefault(x => x.Id == fieldId)
            ?? throw new NotFoundException(fieldId?.ToString() ?? string.Empty, "Field");
        if (cycleId is null) return;
        CropCycle cycle = field.CropCycles.SingleOrDefault(x => x.Id == cycleId)
            ?? throw new NotFoundException(cycleId.Value.ToString(), "Crop cycle");
        if (!posting || cycle.AcceptsOperationalEntries) return;
        if (!authorizedClosedCorrection || string.IsNullOrWhiteSpace(reason))
            throw new ConflictException("Ordinary expenses cannot be posted to a closed or otherwise inactive crop cycle.");
        RequireGrower(context);
    }

    private void Audit(Context context, Guid subjectId, string action, string? reason, string summary,
        Func<AuditEvent, FinanceAuditEventLink> link)
    {
        TenantMembership membership = context.Tenant.Memberships.Single(x => x.UserId == context.UserId);
        var audit = AuditEvent.Create(context.Tenant.Id, context.Farm.Id, "OperationalFinance",
            subjectId, action, context.UserId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), Correlation(), reason, summary);
        finance.Add(audit);
        finance.Add(link(audit));
    }

    private string Correlation() => user.CorrelationId ?? Guid.NewGuid().ToString("N");
    private static decimal Sum(IEnumerable<OperationalCostPosting> values, OperationalCostCategory category) =>
        values.Where(x => x.Category == category).Sum(x => x.AmountUsd);
    private static OperationalTransactionType ParseType(string value) => Parse<OperationalTransactionType>(value, "type");
    private static OperationalFinanceCategory ParseCategory(string value) => Parse<OperationalFinanceCategory>(value, "category");
    private static TransactionAllocationType ParseAllocationType(string value) => Parse<TransactionAllocationType>(value, "allocationType");
    private static T Parse<T>(string value, string field) where T : struct, Enum =>
        Enum.TryParse(value, true, out T parsed) && Enum.IsDefined(parsed) ? parsed : throw Validation(field, $"Unsupported {field}.");
    private static T Apply<T>(Func<T> action, string field)
    { try { return action(); } catch (InvalidOperationException exception) { throw Validation(field, exception.Message); } catch (ArgumentException exception) { throw Validation(field, exception.Message); } }
    private static void Apply(Action action, string field)
    { try { action(); } catch (InvalidOperationException exception) when (exception.Message.Contains("changed after it was loaded", StringComparison.Ordinal)) { throw new ConflictException(exception.Message); } catch (InvalidOperationException exception) { throw Validation(field, exception.Message); } catch (ArgumentException exception) { throw Validation(field, exception.Message); } }
    private static Application.Common.Exceptions.ValidationException Validation(string field, string message) =>
        new([new FluentValidation.Results.ValidationFailure(field, message)]);
    private static void RequireOperator(Context context)
    { if (Role(context) is not (TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager)) throw new ForbiddenAccessException(); }
    private static void RequireGrower(Context context)
    { if (Role(context) != TenantSecurityRoles.Grower) throw new ForbiddenAccessException(); }
    private static string Role(Context context) => context.Tenant.Memberships.Single(x => x.UserId == context.UserId).SecurityRole;
    private static OperationalTransactionDto Map(OperationalTransaction transaction) => new(
        transaction.Id, transaction.Type.ToString(), transaction.Category.ToString(),
        transaction.EventDate.ToString("yyyy-MM-dd"), transaction.PayeeOrPayer, transaction.AmountUsd,
        transaction.SourceReference, transaction.Notes, transaction.Status.ToString(), transaction.Version,
        transaction.CreatedAt, transaction.PostedAt, transaction.IsClosedCycleCorrection,
        transaction.ClosedCycleCorrectionReason, transaction.ReversalOfOperationalTransactionId,
        transaction.ReversalReason, transaction.Allocations.Select(x => new TransactionAllocationDto(
            x.Id, x.CropCycleId, x.FieldId, x.Category.ToString(), x.AmountUsd,
            x.AllocationType.ToString())).ToArray());
    private static CostSourceDto MapSource(OperationalCostPosting posting, CostSourceChain? chain)
    {
        Guid sourceId = posting.PayrollEarningLineId ?? posting.TransactionAllocationId ??
            posting.InputApplicationLineId ?? posting.InventoryLossId ?? Guid.Empty;
        string sourceType = posting.PayrollEarningLineId.HasValue ? "ApprovedPayrollEarning" :
            posting.TransactionAllocationId.HasValue ? "PostedTransactionAllocation" :
            posting.InputApplicationLineId.HasValue ? "ConfirmedInputApplicationLine" :
            "ApprovedInventoryLoss";
        return new(posting.Id, posting.Category.ToString(), posting.AmountUsd, sourceType, sourceId,
            posting.ActivityId, posting.FieldId, posting.CropCycleId,
            posting.ReversalOfOperationalCostPostingId,
            $"{sourceType} {sourceId:N}; posting identity {posting.PostingIdentity}",
            chain?.PayrollRunId, chain?.PayrollCalculationId, chain?.PayrollCalculationVersion,
            chain?.PayrollWorkerLineId, chain?.WorkerProfileId, chain?.WorkRecordId,
            chain?.OperationalTransactionId, chain?.TransactionAllocationId);
    }
    private sealed record Context(Tenant Tenant, Farm Farm, string UserId);
}
