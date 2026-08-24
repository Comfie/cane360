namespace Cane360.Domain.Farms;

using Cane360.Domain.Activities;

public sealed class Tenant : BaseAuditableEntity
{
    private readonly List<TenantMembership> _memberships = [];
    private readonly List<Farm> _farms = [];
    private readonly List<CropVariety> _cropVarieties = [];
    private readonly List<ActivityType> _activityTypes = [];

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
    public IReadOnlyCollection<CropVariety> CropVarieties => _cropVarieties.AsReadOnly();
    public IReadOnlyCollection<ActivityType> ActivityTypes => _activityTypes.AsReadOnly();

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

    public CropVariety AddCropVariety(string code, string name)
    {
        var normalisedCode = code.Trim().ToUpperInvariant();
        if (_cropVarieties.Any(variety =>
            variety.Status == RecordStatus.Active && variety.Code == normalisedCode))
        {
            throw new InvalidOperationException($"Crop variety code '{normalisedCode}' is already in use.");
        }

        var cropVariety = CropVariety.Create(Id, normalisedCode, name);
        _cropVarieties.Add(cropVariety);

        return cropVariety;
    }

    public ActivityType AddActivityType(
        string code,
        string name,
        bool supportsPlanned,
        bool supportsUnplanned,
        ActivityQuantityBasis quantityBasis)
    {
        var normalisedCode = code.Trim().ToUpperInvariant();
        if (_activityTypes.Any(type => type.Status == RecordStatus.Active && type.Code == normalisedCode))
        {
            throw new InvalidOperationException($"Activity type code '{normalisedCode}' is already in use.");
        }

        var activityType = ActivityType.Create(
            Id, normalisedCode, name, supportsPlanned, supportsUnplanned, quantityBasis);
        _activityTypes.Add(activityType);
        return activityType;
    }

    public TenantMembership AddFarmManagerMembership(string userId, Guid personId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (_memberships.Any(membership => membership.Status == RecordStatus.Active &&
            (membership.UserId == userId || membership.PersonId == personId)))
        {
            throw new InvalidOperationException("This user or manager person already has an active tenant membership.");
        }
        var farm = ActiveFarm ?? throw new InvalidOperationException("A FarmManager membership requires an active farm.");
        if (farm.Persons.All(person => person.Id != personId))
            throw new InvalidOperationException("The FarmManager person must belong to this tenant's active farm.");
        var membership = TenantMembership.CreateFarmManager(Id, farm.Id, userId.Trim(), personId);
        _memberships.Add(membership);
        return membership;
    }
}
