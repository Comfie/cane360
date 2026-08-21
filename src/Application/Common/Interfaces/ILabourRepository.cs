using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Common.Interfaces;

public interface ILabourRepository
{
    Task<IReadOnlyList<WorkerProfile>> GetWorkersAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<WorkerProfile?> GetWorkerAsync(Guid tenantId, Guid farmId, Guid workerId, bool trackChanges, CancellationToken cancellationToken);
    Task<bool> HasNationalIdFingerprintAsync(Guid tenantId, Guid farmId, byte[] fingerprint, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkerRate>> GetRatesAsync(Guid tenantId, Guid farmId, Guid workerId, bool trackChanges, CancellationToken cancellationToken);
    Task<Attendance?> GetAttendanceAsync(Guid tenantId, Guid farmId, Guid workerId, DateOnly workDate, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<Attendance>> GetAttendanceRegisterAsync(Guid tenantId, Guid farmId, DateOnly workDate, bool trackChanges, CancellationToken cancellationToken);
    Task<WorkRecord?> GetWorkRecordAsync(Guid tenantId, Guid farmId, Guid workRecordId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkRecord>> GetWorkRecordsAsync(Guid tenantId, Guid farmId, DateOnly? workDate, Guid? workerId, Guid? activityId, bool trackChanges, CancellationToken cancellationToken);
    Task<bool> HasIncompleteWorkForActivityAsync(Guid tenantId, Guid farmId, Guid activityId, CancellationToken cancellationToken);
    Task<bool> HasActiveWorkForAttendanceAsync(Guid tenantId, Guid farmId, Guid attendanceId, CancellationToken cancellationToken);
    void Add(WorkerProfile worker);
    void Add(WorkerRate rate);
    void Add(Attendance attendance);
    void Add(WorkRecord workRecord);
    void Add(AuditEvent auditEvent);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
