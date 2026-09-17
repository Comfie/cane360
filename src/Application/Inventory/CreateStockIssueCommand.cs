using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateStockIssueCommand(
    Guid InputRequestId, DateOnly IssueDate, Guid IssuerPersonId,
    Guid RecipientPersonId, string? LateEntryReason,
    IReadOnlyList<CreateStockIssueLineCommand> Lines) : IRequest<Guid>;
