using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

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
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrower(tenant, userId);
        var receipt = await inventoryRepository.GetReceiptAsync(
            tenant.Id, farm.Id, request.ReceiptId, true, cancellationToken)
            ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        var outcome = Enum.Parse<ApprovalOutcome>(request.Outcome, true);
        var now = timeProvider.GetUtcNow();
        var approval = InventoryAccess.ApplyDomainAction(nameof(request.Outcome), () =>
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
