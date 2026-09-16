namespace Cane360.Application.Inventory;

public sealed record RenameUnitOfMeasureCommand(Guid UnitId, string Name,
    long ExpectedVersion) : IRequest<UnitOfMeasureDto>;
