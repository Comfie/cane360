namespace Cane360.Application.FarmSetup;

public sealed class SaveFarmModelCommandHandler(IFarmSetupRepository repository, IUser user, TimeProvider clock)
    : IRequestHandler<SaveFarmModelCommand, FarmModelDto>
{
    public async Task<FarmModelDto> Handle(SaveFarmModelCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await repository.GetTenantForUserAsync(FarmSetupValidation.RequireUserId(user), false, cancellationToken)
            ?? throw new ForbiddenAccessException();
        if (tenant.ActiveFarm is null) throw FarmSetupValidation.Failure(nameof(request.Id), "Create a farm first.");
        IReadOnlyList<FarmModel> models = await repository.GetFarmModelsAsync(tenant.Id, true, cancellationToken);
        FarmModel model;
        if (request.Id is null)
        {
            if (models.Any(item => item.Code == request.Code.Trim().ToUpperInvariant()))
                throw FarmSetupValidation.Failure(nameof(request.Code), "Farm Model code is already in use.");
            model = FarmModel.Create(tenant.Id, request.Code, request.Name);
            repository.Add(model);
        }
        else
        {
            model = models.SingleOrDefault(item => item.Id == request.Id) ?? throw new NotFoundException(request.Id.ToString()!, "Farm Model");
            if (model.Code != request.Code.Trim().ToUpperInvariant())
                throw FarmSetupValidation.Failure(nameof(request.Code), "Farm Model codes are stable and cannot be changed.");
            try { model.Update(request.Name, request.Active, request.ExpectedVersion ?? 0); }
            catch (InvalidOperationException exception) { throw new ConflictException(exception.Message); }
        }
        FarmProfileAudit.Add(repository, tenant, user, clock, "FarmModelUpdated", "Farm Model reference data created or updated.", nameof(FarmModel), model.Id);
        await repository.SaveChangesAsync(cancellationToken);
        return new FarmModelDto(model.Id, model.Code, model.Name, model.Active, model.Version);
    }
}
