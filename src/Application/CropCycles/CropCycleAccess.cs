using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
using FluentValidation.Results;
using ApplicationValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.CropCycles;

internal static class CropCycleAccess
{
    public static string RequireUserId(IUser user) =>
        user.Id ?? throw new UnauthorizedAccessException();

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

    public static Field RequireField(Tenant tenant, Guid fieldId)
    {
        var farm = tenant.ActiveFarm ?? throw new NotFoundException(tenant.Id.ToString(), "Active farm");
        return farm.Fields.SingleOrDefault(candidate => candidate.Id == fieldId)
            ?? throw new NotFoundException(fieldId.ToString(), "Field");
    }

    public static CropCycle RequireCycle(Field field, Guid cropCycleId) =>
        field.CropCycles.SingleOrDefault(candidate => candidate.Id == cropCycleId)
        ?? throw new NotFoundException(cropCycleId.ToString(), "Crop cycle");

    public static void RequireVersion(CropCycle cycle, long expectedVersion)
    {
        if (cycle.Version != expectedVersion)
        {
            throw new ConflictException(
                "This crop cycle changed after it was loaded. Refresh the page before trying again.");
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
}
