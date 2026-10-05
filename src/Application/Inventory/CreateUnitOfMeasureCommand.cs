namespace Cane360.Application.Inventory;

public sealed record CreateUnitOfMeasureCommand(
    string Code,
    string Name,
    string Dimension,
    int DecimalPlaces) : IRequest<UnitOfMeasureDto>;
