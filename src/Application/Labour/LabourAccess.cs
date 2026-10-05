using Cane360.Domain.Labour;
using FluentValidation.Results;
using ApplicationValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Labour;

internal static class LabourAccess
{
    public static string RequireUserId(IUser user)
    {
        return user.Id ?? throw new UnauthorizedAccessException();
    }

    public static async Task<Tenant> RequireTenantAsync(
        IFarmSetupRepository repository, IUser user, bool trackChanges, CancellationToken cancellationToken)
    {
        string userId = RequireUserId(user);
        return await repository.GetTenantForUserAsync(userId, trackChanges, cancellationToken)
               ?? throw new NotFoundException(userId, "Active grower or farm-manager membership");
    }

    public static Farm RequireFarm(Tenant tenant)
    {
        return tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");
    }

    public static Person RequirePerson(Farm farm, Guid personId)
    {
        return farm.Persons.SingleOrDefault(person => person.Id == personId)
               ?? throw new NotFoundException(personId.ToString(), "Person");
    }

    public static Field RequireField(Farm farm, Guid fieldId)
    {
        return farm.Fields.SingleOrDefault(field => field.Id == fieldId)
               ?? throw new NotFoundException(fieldId.ToString(), "Field");
    }

    public static Activity RequireActivity(Tenant tenant, Guid activityId)
    {
        return tenant.ActiveFarm?.Fields.SelectMany(field => field.CropCycles)
                   .SelectMany(cycle => cycle.Activities)
                   .SingleOrDefault(activity => activity.Id == activityId)
               ?? throw new NotFoundException(activityId.ToString(), "Activity");
    }

    public static CropCycle RequireOperationalCycle(Farm farm, Activity activity)
    {
        CropCycle cycle = farm.Fields.Single(field => field.Id == activity.FieldId).CropCycles
                              .SingleOrDefault(candidate => candidate.Id == activity.CropCycleId)
                          ?? throw new NotFoundException(activity.CropCycleId.ToString(), "Crop cycle");
        if (!cycle.AcceptsOperationalEntries)
        {
            throw Failure(nameof(activity.CropCycleId),
                "Labour evidence requires an Active or Ready-for-harvest crop cycle.");
        }

        return cycle;
    }

    public static WorkerProfile RequireWorker(WorkerProfile? worker, Guid workerId)
    {
        return worker ?? throw new NotFoundException(workerId.ToString(), "Worker");
    }

    public static Attendance RequireAttendance(Attendance? attendance, Guid workerId, DateOnly workDate)
    {
        return attendance ?? throw new NotFoundException($"{workerId}:{workDate:yyyy-MM-dd}", "Attendance");
    }

    public static WorkRecord RequireWorkRecord(WorkRecord? record, Guid workRecordId)
    {
        return record ?? throw new NotFoundException(workRecordId.ToString(), "Work record");
    }

    public static DateOnly HarareDate(DateTimeOffset value)
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, zone).DateTime);
    }

    public static int EntryDelay(DateOnly workDate, DateTimeOffset now)
    {
        return HarareDate(now).DayNumber - workDate.DayNumber;
    }

    public static string SecurityRole(Tenant tenant, string userId)
    {
        return tenant.Memberships.Single(membership => membership.UserId == userId).SecurityRole;
    }

    public static string CorrelationId(IUser user)
    {
        return user.CorrelationId ?? Guid.NewGuid().ToString("N");
    }

    public static ApplicationValidationException Failure(string propertyName, string message)
    {
        return new ApplicationValidationException([new ValidationFailure(propertyName, message)]);
    }

    public static void ApplyDomainAction(string propertyName, Action action)
    {
        try { action(); }
        catch (InvalidOperationException exception) { throw Failure(propertyName, exception.Message); }
        catch (ArgumentException exception) { throw Failure(propertyName, exception.Message); }
    }
}
