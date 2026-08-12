using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed record GetActivityTypesQuery : IRequest<IReadOnlyList<ActivityTypeDto>>;
public sealed record CreateActivityTypeCommand(
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis) : IRequest<ActivityTypeDto>;
public sealed record ArchiveActivityTypeCommand(Guid ActivityTypeId, long ExpectedVersion) : IRequest<ActivityTypeDto>;

public sealed class CreateActivityTypeCommandValidator : AbstractValidator<CreateActivityTypeCommand>
{
    public CreateActivityTypeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(24).Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command).Must(command => command.SupportsPlanned || command.SupportsUnplanned)
            .WithMessage("At least one planning mode is required.");
        RuleFor(command => command.QuantityBasis).IsEnumName(typeof(ActivityQuantityBasis), false);
    }
}

public sealed class GetActivityTypesQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetActivityTypesQuery, IReadOnlyList<ActivityTypeDto>>
{
    public async Task<IReadOnlyList<ActivityTypeDto>> Handle(GetActivityTypesQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        return tenant.ActivityTypes.OrderBy(type => type.Name).Select(Map).ToArray();
    }

    internal static ActivityTypeDto Map(ActivityType type) => new(
        type.Id, type.Code, type.Name, type.SupportsPlanned, type.SupportsUnplanned,
        type.QuantityBasis.ToString(), type.Status.ToString(), type.Version);
}

public sealed class CreateActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<CreateActivityTypeCommand, ActivityTypeDto>
{
    public async Task<ActivityTypeDto> Handle(CreateActivityTypeCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        ActivityType? type = null;
        ActivityAccess.ApplyDomainAction(nameof(request.Code), () => type = tenant.AddActivityType(
            request.Code,
            request.Name,
            request.SupportsPlanned,
            request.SupportsUnplanned,
            Enum.Parse<ActivityQuantityBasis>(request.QuantityBasis)));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type!);
    }
}

public sealed class ArchiveActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<ArchiveActivityTypeCommand, ActivityTypeDto>
{
    public async Task<ActivityTypeDto> Handle(ArchiveActivityTypeCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var type = tenant.ActivityTypes.SingleOrDefault(candidate => candidate.Id == request.ActivityTypeId)
            ?? throw new NotFoundException(request.ActivityTypeId.ToString(), "Activity type");
        if (type.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This activity type changed after it was loaded. Refresh and try again.");
        }
        ActivityAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => type.Archive(request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type);
    }
}

public sealed record GetPersonnelQuery : IRequest<PersonnelRegisterDto>;
public sealed record CreatePersonCommand(
    string DisplayName,
    string? Phone,
    DateOnly ActiveFrom,
    IReadOnlyList<string> Roles,
    bool IsPrimaryManager) : IRequest<PersonnelRegisterDto>;
public sealed record DeactivatePersonCommand(Guid PersonId, long ExpectedVersion, DateOnly ActiveTo) : IRequest<PersonnelRegisterDto>;
public sealed record EndPersonRoleCommand(Guid PersonId, Guid AssignmentId, long ExpectedVersion, DateOnly EffectiveTo) : IRequest<PersonnelRegisterDto>;

public sealed class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.Roles).NotEmpty();
        RuleForEach(command => command.Roles).IsEnumName(typeof(PersonRole), false);
    }
}

public sealed class GetPersonnelQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetPersonnelQuery, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(GetPersonnelQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        return Map(ActivityAccess.RequireFarm(tenant));
    }

    internal static PersonnelRegisterDto Map(Farm farm) => new(
        farm.Persons.Any(person => person.RoleAssignments.Any(role =>
            role.Role == PersonRole.FarmManager && role.IsPrimary && role.EffectiveTo is null)),
        farm.Persons.OrderBy(person => person.DisplayName).Select(person => new PersonDto(
            person.Id,
            person.DisplayName,
            person.Phone,
            person.ActiveFrom.ToString("yyyy-MM-dd"),
            person.ActiveTo?.ToString("yyyy-MM-dd"),
            person.Status.ToString(),
            person.Version,
            person.RoleAssignments.OrderBy(role => role.Role).Select(role => new PersonRoleAssignmentDto(
                role.Id,
                role.Role.ToString(),
                role.IsPrimary,
                role.EffectiveFrom.ToString("yyyy-MM-dd"),
                role.EffectiveTo?.ToString("yyyy-MM-dd"))).ToArray())).ToArray());
}

public sealed class CreatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<CreatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        Person? person = null;
        ActivityAccess.ApplyDomainAction(nameof(request.DisplayName), () =>
        {
            person = farm.AddPerson(request.DisplayName, request.Phone, request.ActiveFrom);
            foreach (var roleName in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = Enum.Parse<PersonRole>(roleName, true);
                farm.AssignRole(person, role, role == PersonRole.FarmManager && request.IsPrimaryManager, request.ActiveFrom);
            }
        });
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}

public sealed class DeactivatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<DeactivatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(DeactivatePersonCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == request.PersonId)
            ?? throw new NotFoundException(request.PersonId.ToString(), "Person");
        if (person.Version != request.ExpectedVersion) throw new ConflictException("This personnel record changed after it was loaded. Refresh and try again.");
        ActivityAccess.ApplyDomainAction(nameof(request.ActiveTo), () => person.Deactivate(request.ActiveTo, request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}

public sealed class EndPersonRoleCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<EndPersonRoleCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(EndPersonRoleCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == request.PersonId)
            ?? throw new NotFoundException(request.PersonId.ToString(), "Person");
        if (person.Version != request.ExpectedVersion) throw new ConflictException("This personnel record changed after it was loaded. Refresh and try again.");
        ActivityAccess.ApplyDomainAction(nameof(request.EffectiveTo), () => person.EndRole(request.AssignmentId, request.EffectiveTo, request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}

public sealed record GetFieldLineProfileQuery(Guid FieldId) : IRequest<FieldLineProfileDto?>;
public sealed record ReplaceFieldLineProfileCommand(
    Guid FieldId,
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    DateOnly EffectiveFrom,
    long? ExpectedVersion) : IRequest<FieldLineProfileDto>;

public sealed class ReplaceFieldLineProfileCommandValidator : AbstractValidator<ReplaceFieldLineProfileCommand>
{
    public ReplaceFieldLineProfileCommandValidator()
    {
        RuleFor(command => command.StandardLineLengthMetres).GreaterThan(0);
        RuleFor(command => command.EstimatedLineCount).GreaterThan(0);
        RuleFor(command => command.NumberingScheme).NotEmpty().MaximumLength(240);
    }
}

public sealed class GetFieldLineProfileQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetFieldLineProfileQuery, FieldLineProfileDto?>
{
    public async Task<FieldLineProfileDto?> Handle(GetFieldLineProfileQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var field = ActivityAccess.RequireField(ActivityAccess.RequireFarm(tenant), request.FieldId);
        return field.CurrentLineProfile is null ? null : Map(field.CurrentLineProfile);
    }

    internal static FieldLineProfileDto Map(FieldLineProfile profile) => new(
        profile.Id,
        profile.FieldId,
        profile.StandardLineLengthMetres,
        profile.EstimatedLineCount,
        profile.NumberingScheme,
        profile.EffectiveFrom.ToString("yyyy-MM-dd"),
        profile.EffectiveTo?.ToString("yyyy-MM-dd"),
        profile.Version);
}

public sealed class ReplaceFieldLineProfileCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<ReplaceFieldLineProfileCommand, FieldLineProfileDto>
{
    public async Task<FieldLineProfileDto> Handle(ReplaceFieldLineProfileCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var field = ActivityAccess.RequireField(ActivityAccess.RequireFarm(tenant), request.FieldId);
        if (field.CurrentLineProfile?.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This line profile changed after it was loaded. Refresh and try again.");
        }
        FieldLineProfile? profile = null;
        ActivityAccess.ApplyDomainAction(nameof(request.EffectiveFrom), () => profile = field.ReplaceLineProfile(
            request.StandardLineLengthMetres, request.EstimatedLineCount, request.NumberingScheme, request.EffectiveFrom));
        await repository.SaveChangesAsync(cancellationToken);
        return GetFieldLineProfileQueryHandler.Map(profile!);
    }
}
