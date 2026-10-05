namespace Cane360.Application.FarmSetup;

public sealed record FarmSetupDto(bool IsConfigured, GrowerDto? Grower, FarmDto? Farm);
