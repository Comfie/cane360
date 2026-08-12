using System.Globalization;
using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record FarmSetupDto(bool IsConfigured, GrowerDto? Grower, FarmDto? Farm);

public sealed record GrowerDto(string DisplayName, string? Phone);

public sealed record FarmDto(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext,
    IReadOnlyList<FieldDto> Fields);

public sealed record FieldDto(
    Guid Id,
    string Code,
    string Name,
    decimal DeclaredHectares,
    decimal? MappedHectares,
    decimal ReportingHectares,
    string ReportingAreaSource,
    string IrrigationMethod,
    string? SoilNotes,
    CropCycleDto? CurrentCropCycle);

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

internal static class FarmSetupMapper
{
    public static FarmSetupDto Map(Tenant? tenant)
    {
        var farm = tenant?.ActiveFarm;
        if (tenant is null || farm is null)
        {
            return new FarmSetupDto(false, null, null);
        }

        return new FarmSetupDto(
            true,
            new GrowerDto(tenant.GrowerProfile.DisplayName, tenant.GrowerProfile.Phone),
            new FarmDto(
                farm.Id,
                farm.Code,
                farm.Name,
                farm.Address,
                farm.Location,
                farm.Tenure,
                farm.DeclaredHectares,
                farm.IrrigationContext,
                farm.Fields
                    .OrderBy(field => field.Code)
                    .Select(MapField)
                    .ToArray()));
    }

    private static FieldDto MapField(Field field)
    {
        var currentCycle = field.CurrentCropCycle;

        return new FieldDto(
            field.Id,
            field.Code,
            field.Name,
            field.DeclaredHectares,
            field.MappedHectares,
            field.ReportingHectares,
            field.ReportingAreaSource.ToString(),
            field.IrrigationMethod,
            field.SoilNotes,
            currentCycle is null ? null : new CropCycleDto(
                currentCycle.Id,
                currentCycle.CycleType.ToString(),
                currentCycle.RatoonNumber,
                currentCycle.Variety,
                FormatDate(currentCycle.StartDate),
                FormatDate(currentCycle.ExpectedHarvestStart),
                FormatDate(currentCycle.ExpectedHarvestEnd),
                currentCycle.ExpectedYieldTonnes,
                currentCycle.Status.ToString()));
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
