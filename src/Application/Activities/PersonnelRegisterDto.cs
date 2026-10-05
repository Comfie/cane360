namespace Cane360.Application.Activities;

public sealed record PersonnelRegisterDto(
    bool PrimaryManagerAssigned,
    IReadOnlyList<PersonDto> Persons);
