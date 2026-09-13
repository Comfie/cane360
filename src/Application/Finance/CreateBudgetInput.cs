namespace Cane360.Application.Finance;

public sealed record CreateBudgetInput(Guid CropCycleId, string Name, decimal? ReportingAreaHa,
    decimal? ExpectedProductionTonnes, string? Notes);
