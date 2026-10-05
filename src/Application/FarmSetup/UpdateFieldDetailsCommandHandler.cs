namespace Cane360.Application.FarmSetup;

public sealed class UpdateFieldDetailsCommandHandler(IFarmSetupRepository repository, IUser user,
    TimeProvider clock) : IRequestHandler<UpdateFieldDetailsCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(UpdateFieldDetailsCommand request, CancellationToken cancellationToken)
    {
        string userId = FarmSetupValidation.RequireUserId(user);
        Tenant tenant = await repository.GetTenantForUserAsync(userId, true, cancellationToken)
            ?? throw new NotFoundException(userId, "Active farm");
        Field field = tenant.ActiveFarm?.Fields.SingleOrDefault(item => item.Id == request.FieldId)
            ?? throw new NotFoundException(request.FieldId.ToString(), "Field");
        field.UpdateDetails(request.Name, request.IrrigationMethod, request.SoilNotes);
        FarmProfileAudit.Add(repository, tenant, user, clock, "FieldDetailsUpdated",
            "Physical field name, irrigation and soil notes updated.", nameof(Field), field.Id);
        await repository.SaveChangesAsync(cancellationToken);
        return FarmSetupMapper.Map(tenant);
    }
}
