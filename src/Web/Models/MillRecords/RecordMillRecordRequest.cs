namespace Cane360.Web.Models.MillRecords;

public sealed record RecordMillRecordRequest(long ExpectedVersion, string IdempotencyKey);
