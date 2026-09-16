namespace Cane360.Application.Inventory;

public sealed record ArchiveUnitOfMeasureCommand(Guid UnitId, long ExpectedVersion)
    : IRequest<UnitOfMeasureDto>;
