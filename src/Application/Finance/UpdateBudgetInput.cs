namespace Cane360.Application.Finance;

public sealed record UpdateBudgetInput(string Name, decimal? ReportingAreaHa,
    decimal? ExpectedProductionTonnes, string? Notes, long ExpectedRowVersion);
