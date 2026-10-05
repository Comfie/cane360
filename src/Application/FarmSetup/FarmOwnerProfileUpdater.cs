namespace Cane360.Application.FarmSetup;

internal static class FarmOwnerProfileUpdater
{
    public static void Apply(Tenant tenant, FarmOwnerProfileInput? profile,
        IFarmSetupRepository repository, IUser user, IWorkerSensitiveDataProtector protector, TimeProvider clock)
    {
        Farm farm = tenant.ActiveFarm!;
        if (profile is not null)
        {
            tenant.GrowerProfile.UpdateProfile(profile.Title, profile.FirstName, profile.Surname, profile.Sex,
                profile.GrowerNumber, profile.Association, profile.MembershipNumber, profile.RegisteredAddress,
                profile.Email, profile.PhotoReference, profile.Active);
            if (!string.IsNullOrWhiteSpace(profile.NationalId))
            {
                ProtectedNationalId protectedId = protector.Protect(tenant.Id, farm.Id,
                    tenant.GrowerProfile.Id, profile.NationalId);
                tenant.GrowerProfile.SetNationalId(protectedId.Ciphertext, protectedId.Nonce, protectedId.Tag,
                    protectedId.KeyId, protectedId.FarmScopedFingerprint, protectedId.DisplayMask);
                FarmProfileAudit.Add(repository, tenant, user, clock, "NationalIdChanged",
                    "Protected Farm Owner national ID changed.");
            }
        }
    }
}
