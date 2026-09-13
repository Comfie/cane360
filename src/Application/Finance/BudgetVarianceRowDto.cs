namespace Cane360.Application.Finance;

public sealed record BudgetVarianceRowDto(string Category, decimal BudgetUsd, decimal ActualUsd,
    decimal VarianceUsd, decimal? VariancePercent, string Status);
