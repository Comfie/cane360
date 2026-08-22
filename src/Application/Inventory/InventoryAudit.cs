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
