namespace Cane360.Application.Administration;

public sealed record AdministrationUserDto(
    Guid MembershipId, string UserId, string? Email, string Role,
    string Status, Guid? PersonId, string? PersonName);
