using System.Globalization;
using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record CropCycleDto(
    Guid Id,
    string CycleType,
    int? RatoonNumber,
    string Variety,
    string StartDate,
    string ExpectedHarvestStart,
    string ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes,
    string Status);
