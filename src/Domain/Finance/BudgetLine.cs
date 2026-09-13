namespace Cane360.Domain.Finance;

public sealed class BudgetLine : BaseEntity
{
    private BudgetLine() { }

    private BudgetLine(Guid tenantId, Guid farmId, Guid budgetId, BudgetCategory category,
        string description, decimal amountUsd, decimal? quantity, string? unit,
        decimal? unitRateUsd, string? notes, DateTimeOffset createdAt)
    {
        TenantId = tenantId;
        FarmId = farmId;
        BudgetId = budgetId;
        SetValues(category, description, amountUsd, quantity, unit, unitRateUsd, notes);
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid BudgetId { get; private set; }
    public BudgetCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal AmountUsd { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public decimal? UnitRateUsd { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static BudgetLine Create(Guid tenantId, Guid farmId, Guid budgetId,
        BudgetCategory category, string description, decimal amountUsd, decimal? quantity,
        string? unit, decimal? unitRateUsd, string? notes, DateTimeOffset createdAt) =>
        new(tenantId, farmId, budgetId, category, description, amountUsd, quantity, unit,
            unitRateUsd, notes, createdAt);

    internal void Update(BudgetCategory category, string description, decimal amountUsd,
        decimal? quantity, string? unit, decimal? unitRateUsd, string? notes) =>
        SetValues(category, description, amountUsd, quantity, unit, unitRateUsd, notes);

    private void SetValues(BudgetCategory category, string description, decimal amountUsd,
        decimal? quantity, string? unit, decimal? unitRateUsd, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amountUsd);
        if (quantity is <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitRateUsd is <= 0) throw new ArgumentOutOfRangeException(nameof(unitRateUsd));
        if (quantity.HasValue != !string.IsNullOrWhiteSpace(unit))
            throw new InvalidOperationException("Quantity and unit must be supplied together.");
        if (quantity.HasValue != unitRateUsd.HasValue)
            throw new InvalidOperationException("Quantity and unit rate must be supplied together.");
        Category = category;
        Description = description.Trim();
        AmountUsd = decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero);
        Quantity = quantity;
        Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        UnitRateUsd = unitRateUsd;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
