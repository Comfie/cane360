namespace Cane360.Application.Administration;

public sealed record FarmSettingDto(
    Guid Id, string Key, string Label, int Value,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, long Version);
