using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record RequestStockIssueCorrectionCommand(
    Guid StockIssueId, long ExpectedVersion, string Reason) : IRequest;
