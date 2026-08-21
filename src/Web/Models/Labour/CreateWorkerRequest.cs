namespace Cane360.Web.Models.Labour;

public sealed record CreateWorkerRequest(
    Guid? PersonId,
    string? DisplayName,
    string? Phone,
    string EmploymentType,
    string ActiveFrom,
    string NationalId);
