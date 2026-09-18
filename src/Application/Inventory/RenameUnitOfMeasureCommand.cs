using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record RenameUnitOfMeasureCommand(Guid UnitId, string Name,
    long ExpectedVersion) : IRequest<UnitOfMeasureDto>;
