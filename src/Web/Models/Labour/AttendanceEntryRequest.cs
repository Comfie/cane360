namespace Cane360.Web.Models.Labour;

public sealed record AttendanceEntryRequest(Guid WorkerId, string Status, Guid? FieldId, long? ExpectedVersion);
