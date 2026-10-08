namespace Cane360.Application.Account;

public sealed record UpdateMyProfileCommand(string DisplayName, string? PhoneNumber) : IRequest<AccountProfileDto>;
