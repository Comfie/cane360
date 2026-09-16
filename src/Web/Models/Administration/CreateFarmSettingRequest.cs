namespace Cane360.Web.Models.Administration;

public sealed record CreateFarmSettingRequest(
    string Key, int Value, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
