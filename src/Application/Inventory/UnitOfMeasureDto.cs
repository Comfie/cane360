namespace Cane360.Application.Inventory;

public sealed record UnitOfMeasureDto(
    Guid Id, string Code, string Name, string Dimension, int DecimalPlaces, string Status, long Version);
