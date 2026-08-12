using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record OpenCropCycleCommand(
    Guid FieldId,
    string CycleType,
    int? RatoonNumber,
    string Variety,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes) : IRequest<FarmSetupDto>;

public sealed class OpenCropCycleCommandValidator : AbstractValidator<OpenCropCycleCommand>
{
    private static readonly string[] CycleTypes = ["PlantCane", "Ratoon"];

    public OpenCropCycleCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
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
        RuleFor(command => command.Variety).NotEmpty().MaximumLength(80);
        RuleFor(command => command.ExpectedHarvestStart)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .WithMessage("Expected harvest must not begin before the crop cycle starts.");
        RuleFor(command => command.ExpectedHarvestEnd)
            .GreaterThanOrEqualTo(command => command.ExpectedHarvestStart)
            .WithMessage("Expected harvest end must be on or after its start.");
        RuleFor(command => command.ExpectedYieldTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}

public sealed class OpenCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<OpenCropCycleCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        OpenCropCycleCommand request,
        CancellationToken cancellationToken)
    {
        var userId = FarmSetupValidation.RequireUserId(user);
        var tenant = await repository.GetTenantForUserAsync(userId, true, cancellationToken);
        var farm = tenant?.ActiveFarm ?? throw new NotFoundException(userId, "Active farm");
        var field = farm.Fields.SingleOrDefault(candidate => candidate.Id == request.FieldId)
            ?? throw new NotFoundException(request.FieldId.ToString(), "Field");

        if (field.CurrentCropCycle is not null)
        {
            throw FarmSetupValidation.Failure(
                nameof(OpenCropCycleCommand.FieldId),
                "This field already has a current crop cycle.");
        }

        field.OpenCurrentCropCycle(
            Enum.Parse<CropCycleType>(request.CycleType, true),
            request.RatoonNumber,
            request.Variety,
            request.StartDate,
            request.ExpectedHarvestStart,
            request.ExpectedHarvestEnd,
            request.ExpectedYieldTonnes);

        await repository.SaveChangesAsync(cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
