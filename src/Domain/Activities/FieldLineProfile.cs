namespace Cane360.Domain.Activities;

public sealed class FieldLineProfile : BaseAuditableEntity
{
    private FieldLineProfile() { }

    private FieldLineProfile(
        Guid fieldId,
        decimal standardLineLengthMetres,
        int estimatedLineCount,
        string numberingScheme,
        DateOnly effectiveFrom)
    {
        FieldId = fieldId;
        StandardLineLengthMetres = standardLineLengthMetres;
        EstimatedLineCount = estimatedLineCount;
        NumberingScheme = numberingScheme.Trim();
        EffectiveFrom = effectiveFrom;
    }

    public Guid FieldId { get; private set; }
    public decimal StandardLineLengthMetres { get; private set; }
    public int EstimatedLineCount { get; private set; }
    public string NumberingScheme { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public long Version { get; private set; }

    internal static FieldLineProfile Create(
        Guid fieldId,
        decimal standardLineLengthMetres,
        int estimatedLineCount,
        string numberingScheme,
        DateOnly effectiveFrom)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(standardLineLengthMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(estimatedLineCount);
        ArgumentException.ThrowIfNullOrWhiteSpace(numberingScheme);
        return new FieldLineProfile(fieldId, standardLineLengthMetres, estimatedLineCount, numberingScheme, effectiveFrom);
    }

    internal void End(DateOnly effectiveTo)
    {
        if (EffectiveTo is not null)
        {
            throw new InvalidOperationException("This line profile has already ended.");
        }

        if (effectiveTo < EffectiveFrom)
        {
            throw new InvalidOperationException("The line-profile end date cannot be before its start date.");
        }

        EffectiveTo = effectiveTo;
        Version++;
    }

    public bool IsEffective(DateOnly onDate) =>
        EffectiveFrom <= onDate && (EffectiveTo is null || EffectiveTo >= onDate);
}
