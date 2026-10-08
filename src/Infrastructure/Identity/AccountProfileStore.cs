using Cane360.Application.Account;
using Cane360.Application.Common.Exceptions;
using Cane360.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Infrastructure.Identity;

public sealed class AccountProfileStore(ApplicationDbContext context) : IAccountProfileStore
{
    private const string DisplayNameClaim = "cane360:display_name";

    public async Task<AccountProfileDto?> GetAsync(string userId, CancellationToken cancellationToken)
    {
        return await context.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new AccountProfileDto(user.Email!, user.EmailConfirmed,
                context.UserClaims.Where(claim => claim.UserId == userId && claim.ClaimType == DisplayNameClaim)
                    .Select(claim => claim.ClaimValue).FirstOrDefault(), user.PhoneNumber))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountProfileDto?> UpdateAsync(string userId, string displayName, string? phoneNumber,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return null;

        IdentityUserClaim<string>? name = await context.UserClaims.SingleOrDefaultAsync(
            claim => claim.UserId == userId && claim.ClaimType == DisplayNameClaim, cancellationToken);
        if (name is null)
        {
            context.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = userId, ClaimType = DisplayNameClaim, ClaimValue = displayName
            });
        }
        else
        {
            name.ClaimValue = displayName;
        }

        if (user.PhoneNumber != phoneNumber)
        {
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = false;
        }

        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Your account changed while saving. Refresh your profile and try again.");
        }
        return new AccountProfileDto(user.Email!, user.EmailConfirmed, displayName, user.PhoneNumber);
    }
}
