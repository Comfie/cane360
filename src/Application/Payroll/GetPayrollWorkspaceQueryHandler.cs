namespace Cane360.Application.Payroll;

public sealed class GetPayrollWorkspaceQueryHandler(IFarmSetupRepository farms, ILabourRepository labour, IUser user) : IRequestHandler<GetPayrollWorkspaceQuery, PayrollWorkspaceDto>
{
    public async Task<PayrollWorkspaceDto> Handle(GetPayrollWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var workers = await labour.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        var workerOptions = workers.Select(worker => new PayrollWorkerOptionDto(worker.Id, farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName, worker.Status.ToString())).OrderBy(worker => worker.DisplayName).ToArray();
        var people = farm.Persons.Where(person => person.Status == RecordStatus.Active).Select(person => new PayrollPersonOptionDto(person.Id, person.DisplayName)).OrderBy(person => person.DisplayName).ToArray();
        return new PayrollWorkspaceDto(PayrollAccess.Role(tenant, userId), workerOptions, people);
    }
}
