using Cane360.Application.Inventory;

namespace Cane360.Application.Administration;

public sealed record AdministrationManagerAccessDto(
    IReadOnlyList<AdministrationManagerCandidateDto> Candidates,
    IReadOnlyList<ManagerInvitationDto> Invitations);
