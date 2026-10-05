namespace Cane360.Application.Inventory;

public sealed class DecideOpeningBalanceCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<DecideOpeningBalanceCommand, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(
        DecideOpeningBalanceCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        StockReceipt receipt = await inventoryRepository.GetReceiptAsync(
                                   tenant.Id, farm.Id, request.ReceiptId, true, cancellationToken)
                               ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        ApprovalOutcome outcome = Enum.Parse<ApprovalOutcome>(request.Outcome, true);
        DateTimeOffset now = timeProvider.GetUtcNow();
        ApprovalDecision approval = InventoryAccess.ApplyDomainAction(nameof(request.Outcome), () =>
            ApprovalDecision.CreateOpeningBalanceDecision(
                tenant.Id, farm.Id, receipt.Id, request.ExpectedVersion, outcome,
                userId, TenantSecurityRoles.Grower, now, request.Reason, request.IdempotencyKey));
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            receipt.RecordOpeningDecision(outcome, request.ExpectedVersion));
        inventoryRepository.Add(approval);
        InventoryAudit.Receipt(inventoryRepository, tenant, farm, user, receipt, "OpeningBalanceDecision",
            now, request.Reason,
            $"Opening balance {outcome.ToString().ToLowerInvariant()} for receipt version {request.ExpectedVersion}.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
