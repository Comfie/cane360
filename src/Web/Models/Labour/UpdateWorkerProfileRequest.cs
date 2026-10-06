using Cane360.Application.Labour;

namespace Cane360.Web.Models.Labour;

public sealed record UpdateWorkerProfileRequest(long ExpectedVersion, long ExpectedPersonVersion,
    string DisplayName, string? Phone, string EmploymentType, WorkerProfileInput Profile);
