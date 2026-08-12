namespace Cane360.Domain.Farms;

public sealed class Tenant : BaseAuditableEntity
{
    private readonly List<TenantMembership> _memberships = [];
    private readonly List<Farm> _farms = [];

    private Tenant() { }

    private Tenant(string userId, string growerDisplayName, string? growerPhone)
    {
        TenantCode = $"GROWER-{Id:N}"[..15].ToUpperInvariant();
        Status = RecordStatus.Active;
        GrowerProfile = GrowerProfile.Create(Id, growerDisplayName, growerPhone);
        _memberships.Add(TenantMembership.CreateGrower(Id, userId));
    }

    public string TenantCode { get; private set; } = string.Empty;
    public RecordStatus Status { get; private set; }
    public GrowerProfile GrowerProfile { get; private set; } = null!;
    public IReadOnlyCollection<TenantMembership> Memberships => _memberships.AsReadOnly();
    public IReadOnlyCollection<Farm> Farms => _farms.AsReadOnly();

    public static Tenant CreateForGrower(string userId, string growerDisplayName, string? growerPhone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(growerDisplayName);

        return new Tenant(userId.Trim(), growerDisplayName.Trim(), growerPhone?.Trim());
    }

    public Farm CreateFarm(
        string code,
        string name,
        string address,
        string location,
        string tenure,
        decimal declaredHectares,
        string irrigationContext)
    {
        if (_farms.Any(farm => farm.Status == RecordStatus.Active))
        {
            throw new InvalidOperationException("A grower tenant can have only one active farm.");
        }

        var farm = Farm.Create(
            Id,
            code,
            name,
            address,
            location,
            tenure,
            declaredHectares,
            irrigationContext);
        _farms.Add(farm);

        return farm;
    }

    public Farm? ActiveFarm => _farms.SingleOrDefault(farm => farm.Status == RecordStatus.Active);
}
