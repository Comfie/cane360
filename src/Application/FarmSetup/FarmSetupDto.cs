using System.Globalization;
using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record FarmSetupDto(bool IsConfigured, GrowerDto? Grower, FarmDto? Farm);
