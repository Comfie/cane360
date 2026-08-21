using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class LabourRepository(ApplicationDbContext context) : ILabourRepository
{
    public async Task<IReadOnlyList<WorkerProfile>> GetWorkersAsync(
        Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.WorkerProfiles.Where(worker => worker.TenantId == tenantId && worker.FarmId == farmId);
        return await Track(query, trackChanges).OrderBy(worker => worker.Status).ThenBy(worker => worker.ActiveFrom).ToListAsync(cancellationToken);
    }

    public Task<WorkerProfile?> GetWorkerAsync(
        Guid tenantId, Guid farmId, Guid workerId, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.WorkerProfiles.Where(worker => worker.TenantId == tenantId && worker.FarmId == farmId && worker.Id == workerId), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> HasNationalIdFingerprintAsync(
        Guid tenantId, Guid farmId, byte[] fingerprint, CancellationToken cancellationToken) =>
        context.WorkerProfiles.AnyAsync(worker =>
            worker.TenantId == tenantId && worker.FarmId == farmId &&
            worker.NationalIdFingerprint.SequenceEqual(fingerprint), cancellationToken);

    public async Task<IReadOnlyList<WorkerRate>> GetRatesAsync(
        Guid tenantId, Guid farmId, Guid workerId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.WorkerRates.Where(rate => rate.TenantId == tenantId && rate.FarmId == farmId && rate.WorkerProfileId == workerId);
        return await Track(query, trackChanges).OrderByDescending(rate => rate.EffectiveFrom).ToListAsync(cancellationToken);
    }

    public Task<Attendance?> GetAttendanceAsync(
        Guid tenantId, Guid farmId, Guid workerId, DateOnly workDate, bool trackChanges, CancellationToken cancellationToken) =>
        Track(context.Attendances.Where(attendance => attendance.TenantId == tenantId && attendance.FarmId == farmId && attendance.WorkerProfileId == workerId && attendance.WorkDate == workDate), trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Attendance>> GetAttendanceRegisterAsync(
        Guid tenantId, Guid farmId, DateOnly workDate, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.Attendances.Where(attendance => attendance.TenantId == tenantId && attendance.FarmId == farmId && attendance.WorkDate == workDate);
        return await Track(query, trackChanges).ToListAsync(cancellationToken);
    }

    public Task<WorkRecord?> GetWorkRecordAsync(
        Guid tenantId, Guid farmId, Guid workRecordId, bool trackChanges, CancellationToken cancellationToken) =>
        IncludeWorkGraph(Track(context.WorkRecords.Where(record => record.TenantId == tenantId && record.FarmId == farmId && record.Id == workRecordId), trackChanges))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkRecord>> GetWorkRecordsAsync(
        Guid tenantId, Guid farmId, DateOnly? workDate, Guid? workerId, Guid? activityId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = context.WorkRecords.Where(record => record.TenantId == tenantId && record.FarmId == farmId);
        if (workDate.HasValue) query = query.Where(record => record.WorkDate == workDate);
        if (workerId.HasValue) query = query.Where(record => record.WorkerProfileId == workerId);
        if (activityId.HasValue) query = query.Where(record => record.Activities.Any(link => link.ActivityId == activityId));
        return await IncludeWorkGraph(Track(query, trackChanges)).AsSplitQuery()
            .OrderByDescending(record => record.WorkDate).ThenByDescending(record => record.EnteredAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasIncompleteWorkForActivityAsync(
        Guid tenantId, Guid farmId, Guid activityId, CancellationToken cancellationToken) =>
        context.WorkRecordActivities.AnyAsync(link =>
            link.TenantId == tenantId && link.FarmId == farmId && link.ActivityId == activityId &&
            link.WorkRecord.Status != WorkRecordStatus.Confirmed &&
            link.WorkRecord.Status != WorkRecordStatus.Cancelled &&
            link.WorkRecord.Status != WorkRecordStatus.Superseded, cancellationToken);

    public Task<bool> HasActiveWorkForAttendanceAsync(
        Guid tenantId, Guid farmId, Guid attendanceId, CancellationToken cancellationToken) =>
        context.WorkRecords.AnyAsync(record =>
            record.TenantId == tenantId && record.FarmId == farmId && record.AttendanceId == attendanceId &&
            record.Status != WorkRecordStatus.Cancelled && record.Status != WorkRecordStatus.Superseded, cancellationToken);

    public void Add(WorkerProfile worker) => context.WorkerProfiles.Add(worker);
    public void Add(WorkerRate rate) => context.WorkerRates.Add(rate);
    public void Add(Attendance attendance) => context.Attendances.Add(attendance);
    public void Add(WorkRecord workRecord) => context.WorkRecords.Add(workRecord);
    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This labour record changed before the action could be completed. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres)
        {
            var conflict = postgres.ConstraintName switch
            {
                "UX_WorkerProfiles_Farm_NationalIdFingerprint" => new ConflictException("A worker with this national ID is already registered on this farm."),
                "UX_Attendances_Worker_WorkDate" => new ConflictException("Attendance already exists for this worker and work date."),
                "UX_WorkRecords_Attendance_TimeBasis" => new ConflictException("This worker already has a daily or monthly record for this attendance and rate basis."),
                "EX_WorkScopes_NoNamedSectionDuplicate" => new ConflictException("This named work section is already claimed for the activity."),
                "IX_WorkerProfiles_FarmId_PersonId" => new ConflictException("This person is already registered as a worker on the farm."),
                "EX_WorkerRates_NoOverlap" => new ConflictException("This rate overlaps another effective rate for the same worker and scope."),
                "EX_WorkScopes_NoLineOverlap" => new ConflictException("One or more standard lines are already claimed for this activity."),
                _ => null
            };
            if (conflict is not null) throw conflict;
            throw;
        }
    }

    private static IQueryable<T> Track<T>(IQueryable<T> query, bool trackChanges) where T : class =>
        trackChanges ? query : query.AsNoTracking();

    private static IQueryable<WorkRecord> IncludeWorkGraph(IQueryable<WorkRecord> query) => query
        .Include(record => record.Activities)
        .Include(record => record.Scopes)
        .Include(record => record.Verification);
}
