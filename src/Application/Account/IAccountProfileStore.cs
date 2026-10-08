namespace Cane360.Application.Account;

public interface IAccountProfileStore
{
    Task<AccountProfileDto?> GetAsync(string userId, CancellationToken cancellationToken);
    Task<AccountProfileDto?> UpdateAsync(string userId, string displayName, string? phoneNumber,
        CancellationToken cancellationToken);
}
