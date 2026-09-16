namespace Cane360.Application.Administration;

using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;

public interface IAdministrationReadRepository
{
    Task<IReadOnlyList<AdministrationUserDto>> GetUsersAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<AdministrationAuditPageDto> GetAuditAsync(Guid tenantId, Guid farmId,
        AdministrationAuditFilter filter, CancellationToken cancellationToken);
    Task<AdministrationAuditDto?> GetAuditEventAsync(Guid tenantId, Guid farmId,
        Guid eventId, CancellationToken cancellationToken);
    Task RecordExportAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentCategory>> GetCategoriesAsync(Guid tenantId, bool trackChanges,
        CancellationToken cancellationToken);
    Task<DocumentCategory?> GetCategoryAsync(Guid tenantId, Guid categoryId, bool trackChanges,
        CancellationToken cancellationToken);
    void Add(DocumentCategory category);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
