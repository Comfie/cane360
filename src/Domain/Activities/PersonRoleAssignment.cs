namespace Cane360.Domain.Activities;

public sealed class PersonRoleAssignment : BaseEntity
{
    private PersonRoleAssignment() { }

    private PersonRoleAssignment(Guid farmId, Guid personId, PersonRole role, bool isPrimary, DateOnly effectiveFrom)
    {
        FarmId = farmId;
        PersonId = personId;
        Role = role;
        IsPrimary = isPrimary;
        EffectiveFrom = effectiveFrom;
    }

    public Guid FarmId { get; private set; }
    public Guid PersonId { get; private set; }
    public PersonRole Role { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    internal static PersonRoleAssignment Create(
        Guid farmId, Guid personId, PersonRole role, bool isPrimary, DateOnly effectiveFrom) =>
        new(farmId, personId, role, isPrimary, effectiveFrom);

    internal void End(DateOnly effectiveTo)
    {
        if (EffectiveTo is not null)
        {
            throw new InvalidOperationException("This role assignment has already ended.");
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new InvalidOperationException("The role end date cannot be before its start date.");
        }

        EffectiveTo = effectiveTo;
    }

    public bool IsEffective(DateOnly onDate) =>
        EffectiveFrom <= onDate && (EffectiveTo is null || EffectiveTo >= onDate);
}
