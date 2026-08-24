namespace Cane360.Domain.Inventory;

public sealed class UnitOfMeasure : BaseAuditableEntity
{
    private UnitOfMeasure() { }

    private UnitOfMeasure(Guid tenantId, string code, string name, string dimension, int decimalPlaces)
    {
        TenantId = tenantId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Dimension = dimension.Trim();
        DecimalPlaces = decimalPlaces;
        Status = InventoryRecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Dimension { get; private set; } = string.Empty;
    public int DecimalPlaces { get; private set; }
    public InventoryRecordStatus Status { get; private set; }
    public long Version { get; private set; }

    public static UnitOfMeasure Create(Guid tenantId, string code, string name, string dimension, int decimalPlaces)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dimension);
        if (decimalPlaces is < 0 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be between zero and six.");
        }

        return new UnitOfMeasure(tenantId, code, name, dimension, decimalPlaces);
    }

    public void Archive(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status == InventoryRecordStatus.Archived) return;
        Status = InventoryRecordStatus.Archived;
        Version++;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This unit changed after it was loaded.");
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
