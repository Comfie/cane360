namespace Cane360.Application.MillRecords;

public sealed record MillFieldDto(Guid Id, string Code, string Name,
    IReadOnlyList<MillCropCycleDto> CropCycles);
