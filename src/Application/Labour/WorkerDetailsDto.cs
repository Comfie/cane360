using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record WorkerDetailsDto(WorkerListItemDto Worker, IReadOnlyList<WorkerRateDto> Rates);
