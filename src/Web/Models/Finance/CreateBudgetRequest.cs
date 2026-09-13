namespace Cane360.Web.Models.Finance;

public sealed record CreateBudgetRequest(Guid CropCycleId, string Name, decimal? ReportingAreaHa,
    decimal? ExpectedProductionTonnes, string? Notes);
