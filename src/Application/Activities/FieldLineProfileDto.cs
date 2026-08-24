using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record FieldLineProfileDto(
    Guid Id,
    Guid FieldId,
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    string EffectiveFrom,
    string? EffectiveTo,
    long Version);
