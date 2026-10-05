namespace Cane360.Application.FarmSetup;

public sealed record FieldDto(
    Guid Id,
    string Code,
    string Name,
    decimal DeclaredHectares,
    decimal? MappedHectares,
    decimal ReportingHectares,
    string ReportingAreaSource,
    string IrrigationMethod,
    string? SoilNotes,
    CropCycleDto? CurrentCropCycle);
