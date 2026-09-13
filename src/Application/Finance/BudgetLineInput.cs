namespace Cane360.Application.Finance;

public sealed record BudgetLineInput(string Category, string Description, decimal AmountUsd,
    decimal? Quantity, string? Unit, decimal? UnitRateUsd, string? Notes, long ExpectedRowVersion);
