using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record PersonnelRegisterDto(
    bool PrimaryManagerAssigned,
    IReadOnlyList<PersonDto> Persons);
