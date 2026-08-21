namespace Cane360.Web.Models.Activities;

public sealed record ReplaceFieldLineProfileRequest(
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    DateOnly EffectiveFrom,
    long? ExpectedVersion);
