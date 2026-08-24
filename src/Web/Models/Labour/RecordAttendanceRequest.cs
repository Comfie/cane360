namespace Cane360.Web.Models.Labour;

public sealed record RecordAttendanceRequest(string WorkDate, string? LateEntryReason, IReadOnlyList<AttendanceEntryRequest> Entries);
