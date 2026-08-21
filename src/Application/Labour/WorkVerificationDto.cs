using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record WorkVerificationDto(
    Guid SupervisorPersonId,
    string SupervisorName,
    DateTimeOffset SupervisorVerifiedAt,
    string Attestation,
    DateTimeOffset? ManagerConfirmedAt,
    string? ManagerConfirmedByUserId);
