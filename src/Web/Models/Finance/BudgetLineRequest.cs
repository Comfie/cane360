namespace Cane360.Web.Models.Finance;

public sealed record BudgetLineRequest(string Category, string Description, decimal AmountUsd,
    decimal? Quantity, string? Unit, decimal? UnitRateUsd, string? Notes,
    long ExpectedRowVersion);
