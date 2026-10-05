namespace Cane360.Application.CropCycles;

public sealed record CropCycleCollectionDto(
    CropCycleFieldDto Field,
    IReadOnlyList<CropCycleListItemDto> CropCycles);
