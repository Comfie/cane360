namespace Cane360.Application.Administration;

public sealed record AdministrationAuditDto(
    Guid Id, DateTimeOffset OccurredAt, string SubjectType, Guid SubjectId,
    string Action, string AuthenticatedUserId, string? AuthenticatedUserEmail,
    Guid? OperationalPersonId, string? OperationalPersonName,
    string SafeSummary, string? Reason, string CorrelationId);
