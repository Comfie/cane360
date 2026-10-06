namespace Cane360.Application.Labour;

public sealed record WorkerDetailsDto(WorkerListItemDto Worker, IReadOnlyList<WorkerRateDto> Rates,
    WorkerProfileInput? Profile = null, long PersonVersion = default);
