namespace Cane360.Application.FarmSetup;

public sealed record GetFarmModelsQuery : IRequest<IReadOnlyList<FarmModelDto>>;
