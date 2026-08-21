namespace Cane360.Web.Models.Activities;

public sealed record CreatePersonRequest(
    string DisplayName,
    string? Phone,
    DateOnly ActiveFrom,
    IReadOnlyList<string> Roles,
    bool IsPrimaryManager);
