using System.Globalization;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Application.Activities;
using Cane360.Domain.Labour;

namespace Cane360.Application.CropCycles;

public sealed record CropCycleListItemDto(
    Guid Id,
    string CycleType,
    int? RatoonNumber,
    Guid? CropVarietyId,
    string Variety,
    string StartDate,
    string ExpectedHarvestStart,
    string ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes,
    string Status,
    long Version,
    HarvestResultDto? HarvestResult);
