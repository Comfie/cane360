namespace Cane360.Application.Finance;

public sealed record CropCycleCostSummaryDto(Guid CropCycleId, Guid FieldId, string FieldName,
    decimal LabourUsd, decimal AppliedInputsUsd, decimal DirectExpensesUsd,
    decimal ApprovedInventoryLossUsd, decimal TotalCostUsd, decimal? ReportingHectares,
    decimal? CostPerHectareUsd, decimal? ActualHarvestedTonnes, decimal? CostPerTonneUsd,
    IReadOnlyCollection<CostSourceDto> Sources);
