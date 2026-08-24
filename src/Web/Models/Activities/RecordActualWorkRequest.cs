namespace Cane360.Web.Models.Activities;

public sealed record RecordActualWorkRequest(
    long ExpectedVersion,
    string ActualAt,
    decimal? ActualQuantity,
    string? LateEntryReason);
