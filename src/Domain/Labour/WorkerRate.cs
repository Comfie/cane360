namespace Cane360.Domain.Labour;

public sealed class WorkerRate : BaseAuditableEntity
{
    private WorkerRate() { }

    private WorkerRate(
        Guid tenantId,
        Guid farmId,
        Guid workerProfileId,
        PayBasis basis,
        Guid? activityTypeId,
        decimal rateUsd,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        TenantId = tenantId;
        FarmId = farmId;
        WorkerProfileId = workerProfileId;
        Basis = basis;
        ActivityTypeId = activityTypeId;
        RateUsd = rateUsd;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public PayBasis Basis { get; private set; }
    public Guid? ActivityTypeId { get; private set; }
    public decimal RateUsd { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public long Version { get; private set; }

    public static WorkerRate Create(
        Guid tenantId,
        Guid farmId,
        Guid workerProfileId,
        PayBasis basis,
        Guid? activityTypeId,
        decimal rateUsd,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateUsd);
        if (effectiveTo < effectiveFrom)
        {
            throw new InvalidOperationException("The rate end date cannot be before its start date.");
        }

        var isPiece = basis is PayBasis.Hectare or PayBasis.StandardLine;
        if (isPiece != activityTypeId.HasValue)
        {
            throw new InvalidOperationException(
                isPiece
                    ? "Piece rates require an activity type."
                    : "Daily and monthly rates cannot be scoped to an activity type.");
        }

        return new WorkerRate(
            tenantId, farmId, workerProfileId, basis, activityTypeId,
            rateUsd, effectiveFrom, effectiveTo);
    }

    public bool AppliesOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);

    public bool Overlaps(WorkerRate other) =>
        WorkerProfileId == other.WorkerProfileId &&
        Basis == other.Basis &&
        ActivityTypeId == other.ActivityTypeId &&
        EffectiveFrom <= (other.EffectiveTo ?? DateOnly.MaxValue) &&
        other.EffectiveFrom <= (EffectiveTo ?? DateOnly.MaxValue);

    public void End(DateOnly effectiveTo, long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This rate changed after it was loaded. Refresh and try again.");
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new InvalidOperationException("The rate end date cannot be before its start date.");
        }

        if (EffectiveTo is not null && effectiveTo > EffectiveTo)
        {
            throw new InvalidOperationException("A closed rate period cannot be extended.");
        }

        EffectiveTo = effectiveTo;
        Version++;
    }
}
