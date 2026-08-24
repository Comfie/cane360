using Cane360.Application.Activities;

namespace Cane360.Application.Inventory;

public sealed record InputControlWorkspaceDto(
    TenantSessionDto Session,
    IReadOnlyList<InventoryApplicationRuleDto> Rules,
    IReadOnlyList<InputRequestDto> Requests,
    IReadOnlyList<StockIssueDto> Issues,
    IReadOnlyList<InventoryItemDto> Items,
    IReadOnlyList<InventoryLotDto> Lots,
    IReadOnlyList<ActivityTypeDto> ActivityTypes,
    IReadOnlyList<PersonDto> People,
    IReadOnlyList<ManagerInvitationDto> Invitations,
    IReadOnlyList<FieldReceiptDto> FieldReceipts,
    IReadOnlyList<InventoryLossDto> Losses,
    IReadOnlyList<InputAccountabilityDto> Accountability);
