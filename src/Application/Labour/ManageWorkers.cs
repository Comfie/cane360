using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record GetWorkersQuery : IRequest<IReadOnlyList<WorkerListItemDto>>;
public sealed record GetWorkerDetailsQuery(Guid WorkerId) : IRequest<WorkerDetailsDto>;
public sealed record CreateWorkerCommand(
    Guid? PersonId,
    string? DisplayName,
    string? Phone,
    string EmploymentType,
    DateOnly ActiveFrom,
    string NationalId) : IRequest<WorkerDetailsDto>;
public sealed record ArchiveWorkerCommand(Guid WorkerId, DateOnly ActiveTo, long ExpectedVersion) : IRequest<WorkerDetailsDto>;
public sealed record RevealWorkerNationalIdCommand(Guid WorkerId, string Reason) : IRequest<RevealedNationalIdDto>;

public sealed class CreateWorkerCommandValidator : AbstractValidator<CreateWorkerCommand>
{
    public CreateWorkerCommandValidator()
    {
        RuleFor(command => command.PersonId).NotEmpty().When(command => command.PersonId.HasValue);
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(120).When(command => !command.PersonId.HasValue);
        RuleFor(command => command.Phone).MaximumLength(30);
        RuleFor(command => command.EmploymentType).IsEnumName(typeof(EmploymentType), false);
        RuleFor(command => command.ActiveFrom).NotEmpty();
        RuleFor(command => command.NationalId).NotEmpty().MaximumLength(80);
    }
}

public sealed class ArchiveWorkerCommandValidator : AbstractValidator<ArchiveWorkerCommand>
{
    public ArchiveWorkerCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.ActiveTo).NotEmpty();
    }
}

public sealed class RevealWorkerNationalIdCommandValidator : AbstractValidator<RevealWorkerNationalIdCommand>
{
    public RevealWorkerNationalIdCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class GetWorkersQueryHandler(IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkersQuery, IReadOnlyList<WorkerListItemDto>>
{
    public async Task<IReadOnlyList<WorkerListItemDto>> Handle(GetWorkersQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return workers.Select(worker => LabourMapper.Worker(farm, worker)).OrderBy(worker => worker.DisplayName).ToArray();
    }
}

public sealed class GetWorkerDetailsQueryHandler(IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkerDetailsQuery, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(GetWorkerDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}

public sealed class CreateWorkerCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IWorkerSensitiveDataProtector protector,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateWorkerCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(CreateWorkerCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, true, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var userId = LabourAccess.RequireUserId(user);
        var person = request.PersonId.HasValue
            ? LabourAccess.RequirePerson(farm, request.PersonId.Value)
            : farm.AddPerson(request.DisplayName!, request.Phone, request.ActiveFrom);
        if (person.Status != RecordStatus.Active || person.ActiveFrom > request.ActiveFrom || person.ActiveTo < request.ActiveFrom)
        {
            throw LabourAccess.Failure(nameof(request.PersonId), "The selected person must be active on the worker start date.");
        }

        var workerId = Guid.NewGuid();
        ProtectedNationalId? protectedId = null;
        LabourAccess.ApplyDomainAction(nameof(request.NationalId), () =>
            protectedId = protector.Protect(tenant.Id, farm.Id, workerId, request.NationalId));
        if (await labourRepository.HasNationalIdFingerprintAsync(
                tenant.Id, farm.Id, protectedId!.FarmScopedFingerprint, cancellationToken))
        {
            throw new ConflictException("A worker with this national ID is already registered on this farm.");
        }

        WorkerProfile? worker = null;
        LabourAccess.ApplyDomainAction(nameof(request.EmploymentType), () => worker = WorkerProfile.Create(
            workerId, tenant.Id, farm.Id, person.Id, Enum.Parse<EmploymentType>(request.EmploymentType, true),
            request.ActiveFrom, protectedId!.Ciphertext, protectedId.Nonce, protectedId.Tag,
            protectedId.KeyId, protectedId.FarmScopedFingerprint, protectedId.DisplayMask));
        labourRepository.Add(worker!);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker!.Id,
            "WorkerRegistered", userId, LabourAccess.SecurityRole(tenant, userId), person.Id,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Worker registered with protected national-ID evidence."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), []);
    }
}

public sealed class ArchiveWorkerCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<ArchiveWorkerCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(ArchiveWorkerCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, true, cancellationToken), request.WorkerId);
        LabourAccess.ApplyDomainAction(nameof(request.ActiveTo), () => worker.Archive(request.ActiveTo, request.ExpectedVersion));
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "WorkerArchived", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Worker archived; historical labour evidence retained."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}

public sealed class RevealWorkerNationalIdCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IWorkerSensitiveDataProtector protector,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<RevealWorkerNationalIdCommand, RevealedNationalIdDto>
{
    public async Task<RevealedNationalIdDto> Handle(RevealWorkerNationalIdCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealRequested", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal requested."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        var nationalId = protector.Reveal(tenant.Id, farm.Id, worker.Id,
            worker.NationalIdCiphertext, worker.NationalIdNonce, worker.NationalIdTag, worker.NationalIdKeyId);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealSucceeded", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal succeeded."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new RevealedNationalIdDto(worker.Id, nationalId);
    }
}
