using Cane360.Domain.Farms;

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
    string IrrigationContext) : IRequest<FarmSetupDto>;
