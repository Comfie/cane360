namespace Cane360.Application.Administration;

public sealed record AdministrationOverviewDto(
    int ActiveUsers, int PendingInvitations, string? ActiveFarmManager,
    int ActiveActivityTypes, int ActiveUnits, int RuleCount,
    int AttentionCount, IReadOnlyList<AdministrationAuditDto> RecentEvents);
