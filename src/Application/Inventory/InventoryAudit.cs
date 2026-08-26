using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

internal static class InventoryAudit
{
    public static void Unit(
        IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, UnitOfMeasure unit,
        string action, DateTimeOffset occurredAt, string summary) =>
        Record(repository, tenant, farm, user, nameof(UnitOfMeasure), unit.Id, action, occurredAt,
            null, null, summary, auditId => InventoryAuditEventLink.ForUnit(auditId, tenant.Id, farm.Id, unit.Id));

    public static void Item(
        IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, InventoryItem item,
        string action, DateTimeOffset occurredAt, string summary) =>
        Record(repository, tenant, farm, user, nameof(InventoryItem), item.Id, action, occurredAt,
            null, null, summary, auditId => InventoryAuditEventLink.ForItem(auditId, tenant.Id, farm.Id, item.Id));

    public static void Supplier(
        IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, Supplier supplier,
        string action, DateTimeOffset occurredAt, string summary) =>
        Record(repository, tenant, farm, user, nameof(Supplier), supplier.Id, action, occurredAt,
            null, null, summary, auditId => InventoryAuditEventLink.ForSupplier(auditId, tenant.Id, farm.Id, supplier.Id));

    public static void Lot(
        IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, InventoryLot lot,
        string action, DateTimeOffset occurredAt, string summary) =>
        Record(repository, tenant, farm, user, nameof(InventoryLot), lot.Id, action, occurredAt,
            null, null, summary, auditId => InventoryAuditEventLink.ForLot(auditId, tenant.Id, farm.Id, lot.Id));

    public static void Receipt(
        IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, StockReceipt receipt,
        string action, DateTimeOffset occurredAt, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(StockReceipt), receipt.Id, action, occurredAt,
            receipt.ReceivedByPersonId, reason, summary,
            auditId => InventoryAuditEventLink.ForReceipt(auditId, tenant.Id, farm.Id, receipt.Id));

    public static void Rule(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        InventoryApplicationRule rule, string action, DateTimeOffset occurredAt, string summary) =>
        Record(repository, tenant, farm, user, nameof(InventoryApplicationRule), rule.Id, action,
            occurredAt, null, null, summary,
            auditId => InventoryAuditEventLink.ForRule(auditId, tenant.Id, farm.Id, rule.Id));

    public static void Request(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        InputRequest request, string action, DateTimeOffset occurredAt, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(InputRequest), request.Id, action,
            occurredAt, null, reason, summary,
            auditId => InventoryAuditEventLink.ForRequest(auditId, tenant.Id, farm.Id, request.Id));

    public static void Issue(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        StockIssue issue, string action, DateTimeOffset occurredAt, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(StockIssue), issue.Id, action,
            occurredAt, issue.IssuerPersonId, reason, summary,
            auditId => InventoryAuditEventLink.ForIssue(auditId, tenant.Id, farm.Id, issue.Id));

    public static void Invitation(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        ManagerInvitation invitation, string action, DateTimeOffset occurredAt, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(ManagerInvitation), invitation.Id, action,
            occurredAt, invitation.PersonId, reason, summary,
            auditId => InventoryAuditEventLink.ForInvitation(auditId, tenant.Id, farm.Id, invitation.Id));

    public static void FieldReceipt(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        FieldReceipt receipt, string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(FieldReceipt), receipt.Id, action, at,
            receipt.RecipientPersonId, reason, summary, id => InventoryAuditEventLink.ForFieldReceipt(id, tenant.Id, farm.Id, receipt.Id));

    public static void Application(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        InputApplication application, string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(InputApplication), application.Id, action, at,
            application.SupervisorPersonId, reason, summary, id => InventoryAuditEventLink.ForApplication(id, tenant.Id, farm.Id, application.Id));

    public static void Return(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        StockReturn stockReturn, string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(StockReturn), stockReturn.Id, action, at,
            stockReturn.ReceiverPersonId, reason, summary, id => InventoryAuditEventLink.ForReturn(id, tenant.Id, farm.Id, stockReturn.Id));

    public static void Loss(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        InventoryLoss loss, string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(InventoryLoss), loss.Id, action, at,
            null, reason, summary, id => InventoryAuditEventLink.ForLoss(id, tenant.Id, farm.Id, loss.Id));

    public static void Correction(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        FieldAccountabilityCorrection correction, string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(FieldAccountabilityCorrection), correction.Id, action, at,
            null, reason, summary, id => InventoryAuditEventLink.ForFieldAccountabilityCorrection(id, tenant.Id, farm.Id, correction.Id));

    public static void Cost(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        OperationalCostPosting posting, string action, DateTimeOffset at, string summary) =>
        Record(repository, tenant, farm, user, nameof(OperationalCostPosting), posting.Id, action, at,
            null, null, summary, id => InventoryAuditEventLink.ForCost(id, tenant.Id, farm.Id, posting.Id));

    public static void Exception(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user,
        ControlException controlException, string action, DateTimeOffset at, string summary) =>
        Record(repository, tenant, farm, user, nameof(ControlException), controlException.Id, action, at,
            null, null, summary, id => InventoryAuditEventLink.ForException(id, tenant.Id, farm.Id, controlException.Id));

    public static void Count(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, StockCount count,
        string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(StockCount), count.Id, action, at, null, reason, summary,
            id => InventoryAuditEventLink.ForStockCount(id, tenant.Id, farm.Id, count.Id));

    public static void Adjustment(IInventoryRepository repository, Tenant tenant, Farm farm, IUser user, StockAdjustment adjustment,
        string action, DateTimeOffset at, string? reason, string summary) =>
        Record(repository, tenant, farm, user, nameof(StockAdjustment), adjustment.Id, action, at, null, reason, summary,
            id => InventoryAuditEventLink.ForStockAdjustment(id, tenant.Id, farm.Id, adjustment.Id));

    private static void Record(
        IInventoryRepository repository,
        Tenant tenant,
        Farm farm,
        IUser user,
        string subjectType,
        Guid subjectId,
        string action,
        DateTimeOffset occurredAt,
        Guid? operationalPersonId,
        string? reason,
        string summary,
        Func<Guid, InventoryAuditEventLink> createLink)
    {
        var userId = InventoryAccess.RequireUserId(user);
        var auditEvent = AuditEvent.Create(
            tenant.Id, farm.Id, subjectType, subjectId, action, userId,
            InventoryAccess.SecurityRole(tenant, userId), operationalPersonId, occurredAt,
            InventoryAccess.CorrelationId(user), reason, summary);
        repository.Add(auditEvent);
        repository.Add(createLink(auditEvent.Id));
    }
}
