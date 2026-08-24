using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using FluentValidation.Results;
using ApplicationValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Inventory;

internal static class InventoryAccess
{
    public static string RequireUserId(IUser user) => user.Id ?? throw new UnauthorizedAccessException();

    public static async Task<Tenant> RequireTenantAsync(
        IFarmSetupRepository repository, IUser user, bool trackChanges, CancellationToken cancellationToken)
    {
        var userId = RequireUserId(user);
        return await repository.GetTenantForUserAsync(userId, trackChanges, cancellationToken)
            ?? throw new NotFoundException(userId, "Active grower or farm-manager membership");
    }

    public static Farm RequireFarm(Tenant tenant) =>
        tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");

    public static string SecurityRole(Tenant tenant, string userId) =>
        tenant.Memberships.Single(membership =>
            membership.UserId == userId && membership.Status == RecordStatus.Active).SecurityRole;

    public static void RequireGrower(Tenant tenant, string userId)
    {
        if (SecurityRole(tenant, userId) != TenantSecurityRoles.Grower)
        {
            throw new ForbiddenAccessException();
        }
    }

    public static void RequireGrowerOrManager(Tenant tenant, string userId)
    {
        if (SecurityRole(tenant, userId) is not (TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager))
            throw new ForbiddenAccessException();
    }

    public static (Field Field, CropCycle Cycle, Activity Activity) RequireOperationalActivity(
        Farm farm, Guid activityId)
    {
        foreach (var field in farm.Fields)
        foreach (var cycle in field.CropCycles)
        {
            var activity = cycle.Activities.SingleOrDefault(candidate => candidate.Id == activityId);
            if (activity is null) continue;
            if (!cycle.AcceptsOperationalEntries || activity.IsTerminal)
                throw Failure(nameof(activityId), "Closed, cancelled, or terminal crop work cannot accept input requests or issues.");
            return (field, cycle, activity);
        }
        throw new NotFoundException(activityId.ToString(), "Activity");
    }

    public static DateOnly OperationalDate(Activity activity) =>
        activity.ActualAt.HasValue ? HarareDate(activity.ActualAt.Value) :
        activity.PlannedDate ?? throw Failure(nameof(activity.Id),
            "The activity needs a planned or actual operational date before inputs can be requested.");

    public static Person RequireActivePerson(Farm farm, Guid personId, string label)
    {
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == personId)
            ?? throw new NotFoundException(personId.ToString(), label);
        if (person.Status != RecordStatus.Active) throw Failure(label, $"The selected {label.ToLowerInvariant()} must be active.");
        return person;
    }

    public static int EntryDelay(DateOnly eventDate, DateTimeOffset now)
    {
        var today = HarareDate(now);
        return today.DayNumber - eventDate.DayNumber;
    }

    public static DateOnly HarareDate(DateTimeOffset value)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, zone).DateTime);
    }

    public static string CorrelationId(IUser user) => user.CorrelationId ?? Guid.NewGuid().ToString("N");

    public static ApplicationValidationException Failure(string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);

    public static T ApplyDomainAction<T>(string propertyName, Func<T> action)
    {
        try { return action(); }
        catch (InvalidOperationException exception) { throw Failure(propertyName, exception.Message); }
        catch (ArgumentException exception) { throw Failure(propertyName, exception.Message); }
    }

    public static void ApplyDomainAction(string propertyName, Action action)
    {
        try { action(); }
        catch (InvalidOperationException exception) { throw Failure(propertyName, exception.Message); }
        catch (ArgumentException exception) { throw Failure(propertyName, exception.Message); }
    }
}
