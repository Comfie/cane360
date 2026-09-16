namespace Cane360.Web.Models.Administration;

public sealed class AdministrationAuditRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Action { get; set; }
    public string? SubjectType { get; set; }
    public string? AuthenticatedUserId { get; set; }
    public Guid? OperationalPersonId { get; set; }
    public string? CorrelationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
