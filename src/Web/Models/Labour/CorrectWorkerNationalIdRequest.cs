namespace Cane360.Web.Models.Labour;

public sealed record CorrectWorkerNationalIdRequest(string NationalId, long ExpectedVersion, string Reason);
