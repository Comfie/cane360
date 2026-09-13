namespace Cane360.Application.Finance;

public sealed record BudgetVarianceReportDto(Guid CropCycleId, Guid FieldId, string FarmName,
    string FieldName, int BudgetVersion, DateTimeOffset BudgetApprovedAt,
    DateTimeOffset ActualCostAsOf, decimal TotalBudgetUsd, decimal TotalActualUsd,
    decimal TotalVarianceUsd, decimal? TotalVariancePercent, string Status,
    decimal? ReportingAreaHa, decimal? BudgetCostPerHectareUsd,
    decimal? ActualCostPerHectareUsd, decimal? ExpectedProductionTonnes,
    decimal? ActualHarvestedTonnes, decimal? BudgetCostPerTonneUsd,
    decimal? ActualCostPerTonneUsd, IReadOnlyList<BudgetVarianceRowDto> Categories,
    IReadOnlyList<CostSourceDto> ActualSources);
