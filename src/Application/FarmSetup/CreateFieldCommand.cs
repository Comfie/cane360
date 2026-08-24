using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record CreateFieldCommand(
    string Code,
    string Name,
    decimal DeclaredHectares,
    decimal? MappedHectares,
    string ReportingAreaSource,
    string IrrigationMethod,
    string? SoilNotes) : IRequest<FarmSetupDto>;
