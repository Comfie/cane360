namespace Cane360.Application.MillRecords;

public sealed record MillInput(string Code, string Name, string? Location, long ExpectedVersion = 0);
