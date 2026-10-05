namespace Cane360.Application.FarmSetup;

public sealed record FarmModelDto(Guid Id, string Code, string Name, bool Active, long Version);
