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
    string IrrigationContext,
    Cane360.Application.FarmSetup.FarmOwnerProfileInput? OwnerProfile = null,
    Guid? FarmModelId = null,
    bool UpdateFarmModel = false) : IRequest<FarmSetupDto>;
