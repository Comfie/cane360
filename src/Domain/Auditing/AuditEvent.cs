namespace Cane360.Domain.Auditing;

public sealed class AuditEvent : BaseEntity
{
    private AuditEvent() { }

    private AuditEvent(
        Guid tenantId,
        Guid farmId,
        string subjectType,
        Guid subjectId,
        string action,
        string authenticatedUserId,
        string securityRole,
        Guid? operationalPersonId,
        DateTimeOffset occurredAt,
        string correlationId,
        string? reason,
        string safeSummary)
    {
        TenantId = tenantId;
        FarmId = farmId;
        SubjectType = subjectType.Trim();
        SubjectId = subjectId;
        Action = action.Trim();
        AuthenticatedUserId = authenticatedUserId.Trim();
        SecurityRole = securityRole.Trim();
        OperationalPersonId = operationalPersonId;
        OccurredAt = occurredAt;
        CorrelationId = correlationId.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        SafeSummary = safeSummary.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string SubjectType { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string AuthenticatedUserId { get; private set; } = string.Empty;
    public string SecurityRole { get; private set; } = string.Empty;
    public Guid? OperationalPersonId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string SafeSummary { get; private set; } = string.Empty;

    public static AuditEvent Create(
        Guid tenantId,
        Guid farmId,
        string subjectType,
        Guid subjectId,
        string action,
        string authenticatedUserId,
        string securityRole,
        Guid? operationalPersonId,
        DateTimeOffset occurredAt,
        string correlationId,
        string? reason,
        string safeSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticatedUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(securityRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(safeSummary);
        return new AuditEvent(
            tenantId, farmId, subjectType, subjectId, action,
            authenticatedUserId, securityRole, operationalPersonId,
            occurredAt, correlationId, reason, safeSummary);
    }
}
