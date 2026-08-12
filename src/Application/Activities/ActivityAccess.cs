using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using FluentValidation.Results;
using ApplicationValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Activities;

internal static class ActivityAccess
{
    public static string RequireUserId(IUser user) => user.Id ?? throw new UnauthorizedAccessException();

    public static async Task<Tenant> RequireTenantAsync(
        IFarmSetupRepository repository,
        IUser user,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(user);
        return await repository.GetTenantForUserAsync(userId, trackChanges, cancellationToken)
            ?? throw new NotFoundException(userId, "Active grower or farm-manager membership");
    }

    public static Farm RequireFarm(Tenant tenant) =>
        tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");

    public static Field RequireField(Farm farm, Guid fieldId) =>
        farm.Fields.SingleOrDefault(field => field.Id == fieldId)
        ?? throw new NotFoundException(fieldId.ToString(), "Field");

    public static CropCycle RequireOperationalCycle(Field field, Guid cropCycleId)
    {
        var cycle = field.CropCycles.SingleOrDefault(candidate => candidate.Id == cropCycleId)
            ?? throw new NotFoundException(cropCycleId.ToString(), "Crop cycle");
        if (!cycle.AcceptsOperationalEntries)
        {
            throw Failure(nameof(cropCycleId), "Activities require an Active or Ready-for-harvest crop cycle.");
        }

        return cycle;
    }

    public static Activity RequireActivity(Tenant tenant, Guid activityId)
    {
        var activity = tenant.ActiveFarm?.Fields
            .SelectMany(field => field.CropCycles)
            .SelectMany(cycle => cycle.Activities)
            .SingleOrDefault(candidate => candidate.Id == activityId);
        return activity ?? throw new NotFoundException(activityId.ToString(), "Activity");
    }

    public static Person RequireSupervisor(Farm farm, Guid personId, DateOnly effectiveDate)
    {
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == personId)
            ?? throw new NotFoundException(personId.ToString(), "Supervisor");
        if (!person.HasEffectiveRole(PersonRole.Supervisor, effectiveDate))
        {
            throw Failure(nameof(personId), "The selected person must have an effective Supervisor role.");
        }

        return person;
    }

    public static void RequireVersion(Activity activity, long expectedVersion)
    {
        if (activity.Version != expectedVersion)
        {
            throw new ConflictException("This activity changed after it was loaded. Refresh the page and try again.");
        }
    }

    public static ApplicationValidationException Failure(string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);

    public static void ApplyDomainAction(string propertyName, Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
        {
            throw Failure(propertyName, exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Failure(propertyName, exception.Message);
        }
    }

    public static DateOnly HarareDate(DateTimeOffset value)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, zone).DateTime);
    }
}
