using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateFieldAccountabilityCorrectionCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateFieldAccountabilityCorrectionCommand, Guid>
{
    public async Task<Guid> Handle(CreateFieldAccountabilityCorrectionCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireFarmManager(tenant, userId);
        if (new[] { command.FieldReceiptId, command.InputApplicationId, command.StockReturnId, command.InventoryLossId }.Count(x => x.HasValue) != 1)
            throw InventoryAccess.Failure(nameof(command), "A correction must identify exactly one original field-accountability record.");
        if (string.IsNullOrWhiteSpace(command.Reason) || string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw InventoryAccess.Failure(nameof(command.Reason), "A correction reason and idempotency key are required.");

        var existing = await inventoryRepository.GetFieldAccountabilityCorrectionByKeyAsync(tenant.Id, farm.Id, command.IdempotencyKey, false, cancellationToken);
        if (existing is not null) return existing.Id;

        Guid activityId;
        FieldAccountabilityCorrection correction;
        if (command.FieldReceiptId.HasValue)
        {
            var receipt = await inventoryRepository.GetFieldReceiptAsync(tenant.Id, farm.Id, command.FieldReceiptId.Value, false, cancellationToken)
                ?? throw new NotFoundException(command.FieldReceiptId.Value.ToString(), "Field receipt");
            if (receipt.Version != command.SourceVersion || receipt.Status != FieldReceiptStatus.Recorded)
                throw new ConflictException("The field receipt is no longer at the version eligible for correction.");
            activityId = receipt.ActivityId;
            correction = FieldAccountabilityCorrection.ForFieldReceipt(tenant.Id, farm.Id, activityId, receipt.Id, receipt.Version, command.Reason, userId, command.IdempotencyKey, timeProvider.GetUtcNow());
        }
        else if (command.InputApplicationId.HasValue)
        {
            var application = await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, command.InputApplicationId.Value, false, cancellationToken)
                ?? throw new NotFoundException(command.InputApplicationId.Value.ToString(), "Input application");
            if (application.Version != command.SourceVersion || application.Status != InputApplicationStatus.ManagerConfirmed)
                throw new ConflictException("Only the current manager-confirmed application can be corrected.");
            activityId = application.ActivityId;
            correction = FieldAccountabilityCorrection.ForApplication(tenant.Id, farm.Id, activityId, application.Id, application.Version, command.Reason, userId, command.IdempotencyKey, timeProvider.GetUtcNow());
        }
        else if (command.StockReturnId.HasValue)
        {
            var stockReturn = await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, command.StockReturnId.Value, false, cancellationToken)
                ?? throw new NotFoundException(command.StockReturnId.Value.ToString(), "Stock return");
            if (stockReturn.Version != command.SourceVersion || stockReturn.Status != StockReturnStatus.Posted)
                throw new ConflictException("Only the current posted return can be corrected.");
            activityId = stockReturn.ActivityId;
            correction = FieldAccountabilityCorrection.ForReturn(tenant.Id, farm.Id, activityId, stockReturn.Id, stockReturn.Version, command.Reason, userId, command.IdempotencyKey, timeProvider.GetUtcNow());
        }
        else
        {
            var loss = await inventoryRepository.GetInventoryLossAsync(tenant.Id, farm.Id, command.InventoryLossId!.Value, false, cancellationToken)
                ?? throw new NotFoundException(command.InventoryLossId.Value.ToString(), "Inventory loss");
            if (loss.Version != command.SourceVersion || loss.Status != InventoryLossStatus.Approved)
                throw new ConflictException("Only the current approved inventory loss can be corrected.");
            activityId = loss.ActivityId;
            correction = FieldAccountabilityCorrection.ForLoss(tenant.Id, farm.Id, activityId, loss.Id, loss.Version, command.Reason, userId, command.IdempotencyKey, timeProvider.GetUtcNow());
        }

        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, activityId, cancellationToken);
        inventoryRepository.Add(correction);
        InventoryAudit.Correction(inventoryRepository, tenant, farm, user, correction, "Requested", timeProvider.GetUtcNow(), command.Reason,
            "Farm manager requested an append-only field-accountability correction.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return correction.Id;
    }
}
