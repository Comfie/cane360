namespace Cane360.Application.Administration;

public sealed record AdministrationCapabilityDto(string Capability,
    bool Grower, bool FarmManager, bool PlatformAdministrator);
