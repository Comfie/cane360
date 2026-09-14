namespace Cane360.Web.Models.MillRecords;

public sealed record MillRequest(string Code, string Name, string? Location,
    long ExpectedVersion);
