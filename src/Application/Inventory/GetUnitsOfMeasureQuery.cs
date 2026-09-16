namespace Cane360.Application.Inventory;

public sealed record GetUnitsOfMeasureQuery : IRequest<IReadOnlyList<UnitOfMeasureDto>>;
