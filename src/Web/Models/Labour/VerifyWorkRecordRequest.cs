namespace Cane360.Web.Models.Labour;

public sealed record VerifyWorkRecordRequest(Guid SupervisorPersonId, long ExpectedVersion);
