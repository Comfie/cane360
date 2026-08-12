namespace Cane360.Web.Models.FarmSetup;

public sealed record CreateGrowerFarmRequest(
    string GrowerDisplayName,
    string? GrowerPhone,
    string FarmCode,
    string FarmName,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext);
