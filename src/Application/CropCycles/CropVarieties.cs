using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record GetCropVarietiesQuery : IRequest<IReadOnlyList<CropVarietyDto>>;

public sealed record CreateCropVarietyCommand(string Code, string Name) : IRequest<CropVarietyDto>;

public sealed class CreateCropVarietyCommandValidator : AbstractValidator<CreateCropVarietyCommand>
{
    public CreateCropVarietyCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(80);
    }
}

public sealed class GetCropVarietiesQueryHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<GetCropVarietiesQuery, IReadOnlyList<CropVarietyDto>>
{
    public async Task<IReadOnlyList<CropVarietyDto>> Handle(
        GetCropVarietiesQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, false, cancellationToken);

        return tenant.CropVarieties
            .Where(variety => variety.Status == RecordStatus.Active)
            .OrderBy(variety => variety.Code)
            .Select(variety => new CropVarietyDto(variety.Id, variety.Code, variety.Name))
            .ToArray();
    }
}

public sealed class CreateCropVarietyCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<CreateCropVarietyCommand, CropVarietyDto>
{
    public async Task<CropVarietyDto> Handle(
        CreateCropVarietyCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, true, cancellationToken);
        CropVariety? variety = null;

        CropCycleAccess.ApplyDomainAction(nameof(request.Code), () =>
            variety = tenant.AddCropVariety(request.Code, request.Name));

        await repository.SaveChangesAsync(cancellationToken);
        return new CropVarietyDto(variety!.Id, variety.Code, variety.Name);
    }
}
