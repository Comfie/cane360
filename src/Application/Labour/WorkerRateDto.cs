namespace Cane360.Application.Labour;

public sealed record WorkerRateDto(
    Guid Id,
    string Basis,
    Guid? ActivityTypeId,
    string? ActivityTypeName,
    decimal RateUsd,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    long Version);
