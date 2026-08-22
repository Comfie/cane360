using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
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
