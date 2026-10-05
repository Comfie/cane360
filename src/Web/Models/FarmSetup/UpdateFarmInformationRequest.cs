namespace Cane360.Web.Models.FarmSetup;

public sealed record UpdateFarmInformationRequest(
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
    bool UpdateFarmModel = false);
