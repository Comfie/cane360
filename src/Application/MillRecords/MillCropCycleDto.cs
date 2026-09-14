namespace Cane360.Application.MillRecords;

public sealed record MillCropCycleDto(Guid Id, string Variety, string Status,
    decimal? ActualHarvestedTonnes);
