namespace Cane360.Application.Finance;

public sealed record BudgetLineDto(Guid Id, string Category, string Description, decimal AmountUsd,
    decimal? Quantity, string? Unit, decimal? UnitRateUsd, string? Notes, DateTimeOffset CreatedAt);
