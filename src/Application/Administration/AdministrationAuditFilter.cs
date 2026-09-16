namespace Cane360.Application.Administration;

public sealed record AdministrationAuditFilter(
    DateTimeOffset? From, DateTimeOffset? To, string? Action, string? SubjectType,
    string? AuthenticatedUserId, Guid? OperationalPersonId, string? CorrelationId,
    int Page, int PageSize);
