using System.Globalization;
using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record FarmDto(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string Location,
    string Tenure,
    decimal DeclaredHectares,
    string IrrigationContext,
    IReadOnlyList<FieldDto> Fields);
