namespace Cane360.Application.Labour;

public sealed record WorkerListItemDto(
    Guid Id,
    Guid PersonId,
    string DisplayName,
    string? Phone,
    string EmploymentType,
    DateOnly ActiveFrom,
    DateOnly? ActiveTo,
    string Status,
    string NationalIdMask,
    long Version);
