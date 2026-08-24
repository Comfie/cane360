namespace Cane360.Application.FarmSetup;

public sealed record UpdateFarmInformationCommand(
    string GrowerDisplayName,
    string? GrowerPhone,
    string FarmCode,
    string FarmName,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext) : IRequest<FarmSetupDto>;
