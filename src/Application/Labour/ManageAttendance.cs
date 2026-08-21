using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record GetAttendanceRegisterQuery(DateOnly WorkDate) : IRequest<AttendanceRegisterDto>;
public sealed record AttendanceEntryCommand(
    Guid WorkerId,
    string Status,
    Guid? FieldId,
    long? ExpectedVersion);
public sealed record RecordAttendanceCommand(
    DateOnly WorkDate,
    string? LateEntryReason,
    IReadOnlyList<AttendanceEntryCommand> Entries) : IRequest<AttendanceRegisterDto>;

public sealed class GetAttendanceRegisterQueryValidator : AbstractValidator<GetAttendanceRegisterQuery>
{
    public GetAttendanceRegisterQueryValidator() => RuleFor(query => query.WorkDate).NotEmpty();
}

public sealed class RecordAttendanceCommandValidator : AbstractValidator<RecordAttendanceCommand>
{
    public RecordAttendanceCommandValidator()
    {
        RuleFor(command => command.WorkDate).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
        RuleFor(command => command.Entries).NotEmpty();
        RuleForEach(command => command.Entries).ChildRules(entry =>
        {
            entry.RuleFor(item => item.WorkerId).NotEmpty();
            entry.RuleFor(item => item.Status).IsEnumName(typeof(AttendanceStatus), false);
            entry.RuleFor(item => item.FieldId).NotEmpty().When(item => string.Equals(item.Status, "Present", StringComparison.OrdinalIgnoreCase));
            entry.RuleFor(item => item.FieldId).Null().When(item => string.Equals(item.Status, "Absent", StringComparison.OrdinalIgnoreCase));
        });
    }
}

public sealed class GetAttendanceRegisterQueryHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetAttendanceRegisterQuery, AttendanceRegisterDto>
{
    public async Task<AttendanceRegisterDto> Handle(GetAttendanceRegisterQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        var attendance = await labourRepository.GetAttendanceRegisterAsync(tenant.Id, farm.Id, request.WorkDate, false, cancellationToken);
        return MapRegister(farm, workers, attendance, request.WorkDate);
    }

    internal static AttendanceRegisterDto MapRegister(
        Farm farm, IReadOnlyList<WorkerProfile> workers, IReadOnlyList<Attendance> attendance, DateOnly workDate)
    {
        var rows = workers.Where(worker => worker.ActiveFrom <= workDate && (worker.ActiveTo is null || worker.ActiveTo >= workDate))
            .Select(worker =>
            {
                var person = farm.Persons.Single(candidate => candidate.Id == worker.PersonId);
                var item = attendance.SingleOrDefault(candidate => candidate.WorkerProfileId == worker.Id);
                var field = item?.FieldId is Guid fieldId ? farm.Fields.Single(candidate => candidate.Id == fieldId) : null;
                return new AttendanceRowDto(worker.Id, person.DisplayName, worker.EmploymentType.ToString(), item?.Id,
                    workDate, item?.Status.ToString(), item?.FieldId, field?.Name, item?.EntryDelayDays ?? 0, item?.Version);
            }).OrderBy(row => row.WorkerName).ToArray();
        return new AttendanceRegisterDto(workDate, rows,
            farm.Fields.Where(field => field.Status == RecordStatus.Active)
                .Select(field => new LabourFieldDto(field.Id, field.Code, field.Name)).OrderBy(field => field.Code).ToArray());
    }
}

public sealed class RecordAttendanceCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<RecordAttendanceCommand, AttendanceRegisterDto>
{
    public async Task<AttendanceRegisterDto> Handle(RecordAttendanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Entries.Select(entry => entry.WorkerId).Distinct().Count() != request.Entries.Count)
        {
            throw LabourAccess.Failure(nameof(request.Entries), "A worker can appear only once in an attendance submission.");
        }

        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var now = timeProvider.GetUtcNow();
        var delay = LabourAccess.EntryDelay(request.WorkDate, now);
        if (delay < 0) throw LabourAccess.Failure(nameof(request.WorkDate), "Attendance cannot be recorded for a future date.");
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        var current = await labourRepository.GetAttendanceRegisterAsync(tenant.Id, farm.Id, request.WorkDate, true, cancellationToken);
        var userId = LabourAccess.RequireUserId(user);

        foreach (var entry in request.Entries)
        {
            var worker = workers.SingleOrDefault(candidate => candidate.Id == entry.WorkerId)
                ?? throw new NotFoundException(entry.WorkerId.ToString(), "Worker");
            if (!worker.IsActiveOn(request.WorkDate))
            {
                throw LabourAccess.Failure(nameof(entry.WorkerId), "The worker is not active on this work date.");
            }

            var status = Enum.Parse<AttendanceStatus>(entry.Status, true);
            if (entry.FieldId.HasValue) LabourAccess.RequireField(farm, entry.FieldId.Value);
            var existing = current.SingleOrDefault(attendance => attendance.WorkerProfileId == entry.WorkerId);
            if (existing is null)
            {
                Attendance? attendance = null;
                LabourAccess.ApplyDomainAction(nameof(entry.Status), () => attendance = Attendance.Create(
                    tenant.Id, farm.Id, worker.Id, request.WorkDate, status, entry.FieldId,
                    now, userId, request.LateEntryReason, delay));
                labourRepository.Add(attendance!);
                current = current.Append(attendance!).ToArray();
                existing = attendance;
            }
            else
            {
                if (!entry.ExpectedVersion.HasValue)
                {
                    throw LabourAccess.Failure(nameof(entry.ExpectedVersion), "Expected version is required when updating attendance.");
                }

                if (await labourRepository.HasActiveWorkForAttendanceAsync(tenant.Id, farm.Id, existing.Id, cancellationToken) &&
                    (status != AttendanceStatus.Present || existing.FieldId != entry.FieldId))
                {
                    throw LabourAccess.Failure(nameof(entry.Status), "Attendance with work records must remain Present on its allocated field.");
                }

                LabourAccess.ApplyDomainAction(nameof(entry.Status), () => existing.Update(
                    status, entry.FieldId, now, userId, request.LateEntryReason, delay, entry.ExpectedVersion.Value));
            }

            labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(Attendance), existing!.Id,
                "AttendanceRecorded", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
                now, LabourAccess.CorrelationId(user), request.LateEntryReason,
                $"{status} attendance recorded for {request.WorkDate:yyyy-MM-dd}."));
        }

        await labourRepository.SaveChangesAsync(cancellationToken);
        return GetAttendanceRegisterQueryHandler.MapRegister(farm, workers, current, request.WorkDate);
    }
}
