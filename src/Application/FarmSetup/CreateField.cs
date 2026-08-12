using Cane360.Domain.Farms;

namespace Cane360.Application.FarmSetup;

public sealed record CreateFieldCommand(
    string Code,
    string Name,
    decimal DeclaredHectares,
    decimal? MappedHectares,
    string ReportingAreaSource,
    string IrrigationMethod,
    string? SoilNotes) : IRequest<FarmSetupDto>;

public sealed class CreateFieldCommandValidator : AbstractValidator<CreateFieldCommand>
{
    private static readonly string[] ReportingSources = ["Declared", "Mapped"];

    public CreateFieldCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$")
            .WithMessage("Field code may contain letters, numbers, underscores, and hyphens.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.DeclaredHectares).GreaterThan(0).LessThanOrEqualTo(100_000);
        RuleFor(command => command.MappedHectares)
            .GreaterThan(0)
            .LessThanOrEqualTo(100_000)
            .When(command => command.MappedHectares.HasValue);
        RuleFor(command => command.ReportingAreaSource)
            .Must(source => ReportingSources.Contains(source, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Reporting area source must be Declared or Mapped.");
        RuleFor(command => command.MappedHectares)
            .NotNull()
            .When(command => string.Equals(command.ReportingAreaSource, "Mapped", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Mapped hectares are required when mapped area is selected for reporting.");
        RuleFor(command => command.IrrigationMethod).NotEmpty().MaximumLength(100);
        RuleFor(command => command.SoilNotes).MaximumLength(500);
    }
}

public sealed class CreateFieldCommandHandler(
    IFarmSetupRepository repository,
    IUser user) : IRequestHandler<CreateFieldCommand, FarmSetupDto>
{
    public async Task<FarmSetupDto> Handle(
        CreateFieldCommand request,
        CancellationToken cancellationToken)
    {
        var userId = FarmSetupValidation.RequireUserId(user);
        var tenant = await repository.GetTenantForUserAsync(userId, true, cancellationToken);
        var farm = tenant?.ActiveFarm ?? throw new NotFoundException(userId, "Active farm");

        if (farm.Fields.Any(field =>
                field.Status == RecordStatus.Active &&
                field.Code.Equals(request.Code.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw FarmSetupValidation.Failure(
                nameof(CreateFieldCommand.Code),
                "This field code is already in use on the farm.");
        }

        farm.AddField(
            request.Code,
            request.Name,
            request.DeclaredHectares,
            request.MappedHectares,
            Enum.Parse<ReportingAreaSource>(request.ReportingAreaSource, true),
            request.IrrigationMethod,
            request.SoilNotes);

        await repository.SaveChangesAsync(cancellationToken);

        return FarmSetupMapper.Map(tenant);
    }
}
