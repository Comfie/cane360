namespace Cane360.Application.Inventory;

public sealed class CreateUnitOfMeasureCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateUnitOfMeasureCommand, UnitOfMeasureDto>
{
    public async Task<UnitOfMeasureDto> Handle(
        CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        UnitOfMeasure unit = UnitOfMeasure.Create(tenant.Id, request.Code, request.Name, request.Dimension,
            request.DecimalPlaces);
        inventoryRepository.Add(unit);
        InventoryAudit.Unit(inventoryRepository, tenant, farm, user, unit, "Created", timeProvider.GetUtcNow(),
            $"Created stock unit {unit.Code} with {unit.DecimalPlaces} decimal places.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Unit(unit);
    }
}
