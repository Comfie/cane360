namespace Cane360.Application.Labour;

public sealed class GetLabourReferenceDataQueryHandler(IFarmSetupRepository farmRepository, IUser user)
    : IRequestHandler<GetLabourReferenceDataQuery, LabourReferenceDataDto>
{
    public async Task<LabourReferenceDataDto> Handle(GetLabourReferenceDataQuery request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        LabourActivityDto[] activities = farm.Fields.SelectMany(field => field.CropCycles)
            .Where(cycle => cycle.AcceptsOperationalEntries)
            .SelectMany(cycle => cycle.Activities)
            .Where(activity => activity.ActualAt.HasValue &&
                               LabourAccess.HarareDate(activity.ActualAt.Value) == request.WorkDate &&
                               !activity.IsTerminal)
            .Select(activity => new LabourActivityDto(activity.Id, activity.ActivityTypeId, activity.ActivityTypeName,
                activity.FieldId,
                request.WorkDate, activity.QuantityBasis.ToString(), activity.Status.ToString())).ToArray();
        return new LabourReferenceDataDto(
            farm.Fields.Where(field => field.Status == RecordStatus.Active)
                .Select(field => new LabourFieldDto(field.Id, field.Code, field.Name)).ToArray(),
            activities,
            farm.Persons.Where(person => person.HasEffectiveRole(PersonRole.Supervisor, request.WorkDate))
                .Select(person => new LabourPersonDto(person.Id, person.DisplayName)).ToArray());
    }
}
