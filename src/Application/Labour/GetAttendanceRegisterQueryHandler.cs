using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

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
