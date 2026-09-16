namespace Cane360.Domain.Farms;

public sealed class FarmSetting : BaseAuditableEntity
{
    public const string ActivityLateEntryReasonDays = "ActivityLateEntryReasonDays";

    private FarmSetting() { }

    private FarmSetting(Guid tenantId, Guid farmId, string key, int value,
        DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        TenantId = tenantId;
        FarmId = farmId;
        Key = key;
        Value = value;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public int Value { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public long Version { get; private set; }

    public static FarmSetting Create(Guid tenantId, Guid farmId, string key, int value,
        DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (key != ActivityLateEntryReasonDays)
            throw new InvalidOperationException("This farm setting is not supported.");
        if (value is < 0 or > 30)
            throw new InvalidOperationException("Late-entry days must be between 0 and 30.");
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new InvalidOperationException("Setting end date cannot precede its start date.");
        return new FarmSetting(tenantId, farmId, key, value, effectiveFrom, effectiveTo);
    }

    public bool IsEffective(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);

    public void End(DateOnly effectiveTo, long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This setting changed after it was loaded.");
        if (effectiveTo < EffectiveFrom || (EffectiveTo.HasValue && effectiveTo > EffectiveTo))
            throw new InvalidOperationException("The end date must stay within the setting's effective range.");
        EffectiveTo = effectiveTo;
        Version++;
    }
}
