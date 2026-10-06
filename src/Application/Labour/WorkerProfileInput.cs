namespace Cane360.Application.Labour;

public sealed record WorkerProfileInput(
    string? EmployeeNumber = null,
    string? Title = null,
    string? FirstName = null,
    string? Surname = null,
    string? Sex = null,
    string? Address = null,
    string? PhotoReference = null,
    string? NextOfKinName = null,
    string? NextOfKinRelationship = null,
    string? NextOfKinPhone = null,
    string? NextOfKinAddress = null,
    DateOnly? DateOfBirth = null);
