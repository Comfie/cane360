using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record ActivateCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion) : IRequest<CropCycleDetailsDto>;

public sealed record CancelCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion,
    string Reason) : IRequest<CropCycleDetailsDto>;

public sealed record MarkCropCycleReadyForHarvestCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion) : IRequest<CropCycleDetailsDto>;

public sealed record HarvestCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion,
    DateOnly HarvestDate,
    decimal ActualTonnes) : IRequest<CropCycleDetailsDto>;

public sealed record CloseCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion) : IRequest<CropCycleDetailsDto>;

public sealed class CancelCropCycleCommandValidator : AbstractValidator<CancelCropCycleCommand>
{
    public CancelCropCycleCommandValidator() =>
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
}

public sealed class HarvestCropCycleCommandValidator : AbstractValidator<HarvestCropCycleCommand>
{
    public HarvestCropCycleCommandValidator()
    {
        RuleFor(command => command.HarvestDate).NotEmpty();
        RuleFor(command => command.ActualTonnes).GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }
}

public sealed class ActivateCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<ActivateCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(ActivateCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await LoadAsync(repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            field.ActivateCropCycle(cycle, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }

    internal static async Task<(Field Field, CropCycle Cycle, string UserId)> LoadAsync(
        IFarmSetupRepository repository,
        IUser user,
        Guid fieldId,
        Guid cropCycleId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var tenant = await CropCycleAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var field = CropCycleAccess.RequireField(tenant, fieldId);
        var cycle = CropCycleAccess.RequireCycle(field, cropCycleId);
        CropCycleAccess.RequireVersion(cycle, expectedVersion);
        return (field, cycle, CropCycleAccess.RequireUserId(user));
    }
}

public sealed class CancelCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CancelCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(CancelCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.Reason), () =>
            cycle.Cancel(request.Reason, timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}

public sealed class MarkCropCycleReadyForHarvestCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<MarkCropCycleReadyForHarvestCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(MarkCropCycleReadyForHarvestCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            cycle.MarkReadyForHarvest(timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}

public sealed class HarvestCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<HarvestCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(HarvestCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        CropCycleAccess.ApplyDomainAction(nameof(request.HarvestDate), () =>
            cycle.RecordHarvest(request.HarvestDate, request.ActualTonnes, today, now, userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}

public sealed class CloseCropCycleCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CloseCropCycleCommand, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(CloseCropCycleCommand request, CancellationToken cancellationToken)
    {
        var (field, cycle, userId) = await ActivateCropCycleCommandHandler.LoadAsync(
            repository, user, request.FieldId, request.CropCycleId, request.ExpectedVersion, cancellationToken);
        CropCycleAccess.ApplyDomainAction(nameof(request.CropCycleId), () =>
            cycle.Close(timeProvider.GetUtcNow(), userId));
        await repository.SaveChangesAsync(cancellationToken);
        return CropCycleMapper.MapDetails(field, cycle);
    }
}
