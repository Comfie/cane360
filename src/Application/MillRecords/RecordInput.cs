namespace Cane360.Application.MillRecords;

public sealed record RecordInput(long ExpectedVersion, string IdempotencyKey);
