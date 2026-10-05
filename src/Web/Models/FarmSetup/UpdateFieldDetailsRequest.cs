namespace Cane360.Web.Models.FarmSetup;

public sealed record UpdateFieldDetailsRequest(string Name, string IrrigationMethod, string? SoilNotes);
