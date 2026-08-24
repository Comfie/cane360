namespace Cane360.Web.Models.Activities;

public sealed record UpdatePersonRequest(
    string DisplayName,
    string? Phone,
    string Role,
    bool IsPrimaryManager,
    DateOnly RoleEffectiveFrom,
    long ExpectedVersion);
