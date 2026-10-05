namespace Cane360.Application.Labour;

public sealed record GetWorkersQuery : IRequest<IReadOnlyList<WorkerListItemDto>>;
