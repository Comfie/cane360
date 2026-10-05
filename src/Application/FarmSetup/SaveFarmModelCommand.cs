namespace Cane360.Application.FarmSetup;

public sealed record SaveFarmModelCommand(string Code, string Name, bool Active = true,
    Guid? Id = null, long? ExpectedVersion = null) : IRequest<FarmModelDto>;
