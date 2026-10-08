namespace Cane360.Application.Account;

public sealed class UpdateMyProfileCommandHandler(IAccountProfileStore profiles, IUser user)
    : IRequestHandler<UpdateMyProfileCommand, AccountProfileDto>
{
    public async Task<AccountProfileDto> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        string? phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        return await profiles.UpdateAsync(userId, request.DisplayName.Trim(), phone, cancellationToken)
            ?? throw new UnauthorizedAccessException();
    }
}
