namespace Cane360.Web.Models.Finance;

public sealed record UpdateBudgetRequest(string Name, decimal? ReportingAreaHa,
    decimal? ExpectedProductionTonnes, string? Notes, long ExpectedRowVersion);
