namespace Cane360.Domain.Inventory;

public sealed class InventoryCategory : BaseAuditableEntity
{
    private InventoryCategory() { }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string NormalizedCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool Active { get; private set; } = true;
    public long Version { get; private set; } = 1;

    public static InventoryCategory Create(Guid tenantId, string code, string name,
        string? description = null, int displayOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (tenantId == Guid.Empty || code.Trim().Length > 40 ||
            !System.Text.RegularExpressions.Regex.IsMatch(code.Trim(), "^[A-Za-z0-9_-]+$"))
        {
            throw new ArgumentException("Category requires a tenant and a code of at most 40 letters, digits, underscores or hyphens.");
        }

        InventoryCategory category = new() { TenantId = tenantId, Code = code.Trim().ToUpperInvariant() };
        category.SetDetails(name, description, displayOrder);
        return category;
    }

    public static InventoryCategory CreateLegacy(Guid tenantId, InventoryItemCategory legacy)
    {
        string name = legacy == InventoryItemCategory.SeedAndPlantingMaterial
            ? "Seed And Planting Material" : legacy.ToString();
        InventoryCategory category = Create(tenantId, legacy.ToString(), name, null, (int)legacy);
        category.Code = legacy.ToString();
        return category;
    }

    public void Update(string name, string? description, int displayOrder, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        SetDetails(name, description, displayOrder);
        Version++;
    }

    public void SetActive(bool active, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Active == active)
        {
            return;
        }

        Active = active;
        Version++;
    }

    private void SetDetails(string name, string? description, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 120 || description?.Trim().Length > 500 || displayOrder is < 0 or > 10000)
        {
            throw new ArgumentException("Category name (120), description (500) or display order (0–10000) exceeds its limit.");
        }

        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DisplayOrder = displayOrder;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This inventory category changed after it was loaded.");
        }
    }
}
