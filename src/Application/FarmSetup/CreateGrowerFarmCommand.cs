namespace Cane360.Application.FarmSetup;

public sealed record CreateGrowerFarmCommand(
    string GrowerDisplayName,
    string? GrowerPhone,
    string FarmCode,
    string FarmName,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext,
    Cane360.Application.FarmSetup.FarmOwnerProfileInput? OwnerProfile = null) : IRequest<FarmSetupDto>;
