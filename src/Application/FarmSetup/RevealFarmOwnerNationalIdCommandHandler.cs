namespace Cane360.Application.FarmSetup;

public sealed class RevealFarmOwnerNationalIdCommandHandler(IFarmSetupRepository repository,
    IUser user, IWorkerSensitiveDataProtector protector, TimeProvider clock)
    : IRequestHandler<RevealFarmOwnerNationalIdCommand, RevealedFarmOwnerNationalIdDto>
{
    public async Task<RevealedFarmOwnerNationalIdDto> Handle(RevealFarmOwnerNationalIdCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await repository.GetTenantForUserAsync(FarmSetupValidation.RequireUserId(user), false, cancellationToken)
            ?? throw new ForbiddenAccessException();
        if (!tenant.Memberships.Any(item => item.UserId == user.Id && item.Status == RecordStatus.Active &&
            item.SecurityRole == TenantSecurityRoles.Grower)) throw new ForbiddenAccessException();
        GrowerProfile profile = tenant.GrowerProfile;
        Farm farm = tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");
        if (profile.NationalIdCiphertext is null) throw new NotFoundException(profile.Id.ToString(), "National ID");
        FarmProfileAudit.Add(repository, tenant, user, clock, "NationalIdRevealRequested", "Authorised full national-ID reveal requested.");
        await repository.SaveChangesAsync(cancellationToken);
        string nationalId = protector.Reveal(tenant.Id, farm.Id, profile.Id, profile.NationalIdCiphertext,
            profile.NationalIdNonce!, profile.NationalIdTag!, profile.NationalIdKeyId!);
        FarmProfileAudit.Add(repository, tenant, user, clock, "NationalIdRevealSucceeded", "Authorised full national-ID reveal succeeded.");
        await repository.SaveChangesAsync(cancellationToken);
        return new RevealedFarmOwnerNationalIdDto(profile.Id, nationalId);
    }
}
