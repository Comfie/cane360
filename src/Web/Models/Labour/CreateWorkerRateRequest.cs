namespace Cane360.Web.Models.Labour;

public sealed record CreateWorkerRateRequest(
    string Basis,
    Guid? ActivityTypeId,
    decimal RateUsd,
    string EffectiveFrom,
    string? EffectiveTo);
