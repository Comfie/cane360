using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

internal static class WorkerProfileUpdater
{
    public static void Apply(WorkerProfile worker, WorkerProfileInput profile, EmploymentType type,
        TimeProvider clock, long expectedVersion)
    {
        worker.UpdateProfile(profile.EmployeeNumber, profile.Title, profile.FirstName, profile.Surname,
            profile.Sex, profile.DateOfBirth, profile.Address, profile.PhotoReference, profile.NextOfKinName,
            profile.NextOfKinRelationship, profile.NextOfKinPhone, profile.NextOfKinAddress,
            type, LabourAccess.HarareDate(clock.GetUtcNow()), expectedVersion);
    }

    public static string Name(WorkerProfileInput? profile, string? displayName) =>
        !string.IsNullOrWhiteSpace(profile?.FirstName) && !string.IsNullOrWhiteSpace(profile.Surname)
            ? $"{profile.FirstName.Trim()} {profile.Surname.Trim()}" : displayName!;
}
