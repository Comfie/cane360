namespace Cane360.Web.Models.FarmSetup;

public sealed record CreateFieldRequest(
    string Code,
    string Name,
    decimal DeclaredHectares,
    decimal? MappedHectares,
    string ReportingAreaSource,
    string IrrigationMethod,
    string? SoilNotes);
