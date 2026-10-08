namespace Cane360.Application.Account;

public sealed class GetMyProfileQueryHandler(IAccountProfileStore profiles, IUser user)
    : IRequestHandler<GetMyProfileQuery, AccountProfileDto>
{
    public async Task<AccountProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        return await profiles.GetAsync(userId, cancellationToken) ?? throw new UnauthorizedAccessException();
    }
}
