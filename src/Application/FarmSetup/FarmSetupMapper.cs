using System.Globalization;

namespace Cane360.Application.FarmSetup;

internal static class FarmSetupMapper
{
    public static FarmSetupDto Map(Tenant? tenant)
    {
        Farm? farm = tenant?.ActiveFarm;
        if (tenant is null || farm is null)
        {
            return new FarmSetupDto(false, null, null);
        }

        return new FarmSetupDto(
            true,
            new GrowerDto(tenant.GrowerProfile.DisplayName, tenant.GrowerProfile.Phone,
                tenant.GrowerProfile.Title, tenant.GrowerProfile.FirstName, tenant.GrowerProfile.Surname, tenant.GrowerProfile.Sex, tenant.GrowerProfile.GrowerNumber, tenant.GrowerProfile.Association, tenant.GrowerProfile.MembershipNumber, tenant.GrowerProfile.RegisteredAddress, tenant.GrowerProfile.Email, tenant.GrowerProfile.PhotoReference,
                tenant.GrowerProfile.Active, tenant.GrowerProfile.NationalIdMask),
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
                    .ToArray(), farm.FarmModelId));
    }

    private static FieldDto MapField(Field field)
    {
        CropCycle? currentCycle = field.CurrentCropCycle;

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
            currentCycle is null
                ? null
                : new CropCycleDto(
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

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
