namespace Cane360.Application.Account;

public sealed record AccountProfileDto(string Email, bool IsEmailConfirmed, string? DisplayName, string? PhoneNumber);
