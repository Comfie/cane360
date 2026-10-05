namespace Cane360.Application.FarmSetup;

public sealed record FarmOwnerProfileInput(
    string? Title = null,
    string? FirstName = null,
    string? Surname = null,
    string? Sex = null,
    string? GrowerNumber = null,
    string? Association = null,
    string? MembershipNumber = null,
    string? RegisteredAddress = null,
    string? Email = null,
    string? PhotoReference = null,
    bool Active = true,
    string? NationalId = null);
