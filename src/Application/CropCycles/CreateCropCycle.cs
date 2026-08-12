using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record CreateCropCycleCommand(
    Guid FieldId,
    string CycleType,
    int? RatoonNumber,
    Guid CropVarietyId,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes) : IRequest<CropCycleDetailsDto>;

public sealed class CreateCropCycleCommandValidator : AbstractValidator<CreateCropCycleCommand>
{
    private static readonly string[] CycleTypes = ["PlantCane", "Ratoon"];

    public CreateCropCycleCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropVarietyId).NotEmpty();
        RuleFor(command => command.CycleType)
            .Must(type => CycleTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Cycle type must be PlantCane or Ratoon.");
        RuleFor(command => command.RatoonNumber)
            .NotNull()
            .GreaterThanOrEqualTo(1)
            .When(command => string.Equals(command.CycleType, "Ratoon", StringComparison.OrdinalIgnoreCase))
            .WithMessage("A ratoon number is required for a ratoon crop cycle.");
        RuleFor(command => command.RatoonNumber)
            .Null()
            .When(command => string.Equals(command.CycleType, "PlantCane", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Plant cane cannot carry a ratoon number.");
        RuleFor(command => command.ExpectedHarvestStart)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("Expected harvest must not begin before the crop cycle starts.");
        RuleFor(command => command.ExpectedHarvestEnd)
            .GreaterThanOrEqualTo(command => command.ExpectedHarvestStart)
            .WithMessage("Expected harvest end must be on or after its start.");
        RuleFor(command => command.ExpectedYieldTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}

public sealed class CreateCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(
        CreateCropCycleCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, true, cancellationToken);
        var field = CropCycleAccess.RequireField(tenant, request.FieldId);
        var variety = tenant.CropVarieties.SingleOrDefault(candidate =>
            candidate.Id == request.CropVarietyId && candidate.Status == RecordStatus.Active)
            ?? throw new NotFoundException(request.CropVarietyId.ToString(), "Active crop variety");
        var userId = CropCycleAccess.RequireUserId(user);
        CropCycle? cropCycle = null;

        CropCycleAccess.ApplyDomainAction(nameof(request.CycleType), () =>
            cropCycle = field.CreateCropCycleDraft(
                Enum.Parse<CropCycleType>(request.CycleType, true),
                request.RatoonNumber,
                variety,
                variety.Name,
                request.StartDate,
                request.ExpectedHarvestStart,
                request.ExpectedHarvestEnd,
                request.ExpectedYieldTonnes,
                timeProvider.GetUtcNow(),
                userId));

        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cropCycle!);
    }
}
