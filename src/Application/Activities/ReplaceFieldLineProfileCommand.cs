namespace Cane360.Application.Activities;

public sealed record ReplaceFieldLineProfileCommand(
    Guid FieldId,
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    DateOnly EffectiveFrom,
    long? ExpectedVersion) : IRequest<FieldLineProfileDto>;
