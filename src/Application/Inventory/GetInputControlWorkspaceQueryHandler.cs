using System.Globalization;
using Cane360.Application.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Inventory;

public sealed class GetInputControlWorkspaceQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetInputControlWorkspaceQuery, InputControlWorkspaceDto>
{
    public async Task<InputControlWorkspaceDto> Handle(
        GetInputControlWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        var membership = tenant.Memberships.Single(item => item.UserId == userId && item.Status == RecordStatus.Active);
        var rules = await inventoryRepository.GetRulesAsync(tenant.Id, farm.Id, cancellationToken);
        var requests = await inventoryRepository.GetInputRequestsAsync(tenant.Id, farm.Id, request.ActivityId, false, cancellationToken);
        var issues = await inventoryRepository.GetStockIssuesAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        var items = await inventoryRepository.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        var lots = await inventoryRepository.GetLotsAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        var invitations = await inventoryRepository.GetManagerInvitationsAsync(tenant.Id, farm.Id, false, cancellationToken);

        var requestDtos = new List<InputRequestDto>();
        foreach (var inputRequest in requests)
        {
            var context = FindActivity(farm, inputRequest.ActivityId);
            var lineDtos = new List<InputRequestLineDto>();
            foreach (var line in inputRequest.Lines.OrderBy(candidate => candidate.LineNumber))
            {
                var live = await inventoryRepository.GetItemStockSnapshotAsync(tenant.Id, farm.Id, line.InventoryItemId, cancellationToken);
                var issued = await inventoryRepository.GetPostedIssueQuantityAsync(line.Id, cancellationToken);
                lineDtos.Add(new InputRequestLineDto(line.Id, line.InventoryItemId, line.ItemCodeSnapshot,
                    line.ItemNameSnapshot, line.UnitCodeSnapshot, line.InventoryApplicationRuleId,
                    line.RuleVersionSnapshot, line.CoverageBasisSnapshot.ToString(), line.PlannedCoverage,
                    line.PlannedRate, line.PlannedQuantity, line.RequestedQuantity,
                    decimal.Round(line.PlannedQuantity * (1 - line.LowerTolerancePercent / 100m), 6),
                    decimal.Round(line.PlannedQuantity * (1 + line.UpperTolerancePercent / 100m), 6),
                    line.ApprovalRequirement.ToString(), line.AvailableQuantitySnapshot, live.Quantity,
                    line.EstimatedUnitCostUsdSnapshot, line.EstimatedValueUsdSnapshot, issued,
                    decimal.Max(0, line.RequestedQuantity - issued)));
            }
            requestDtos.Add(new InputRequestDto(inputRequest.Id, inputRequest.FieldId, inputRequest.CropCycleId,
                inputRequest.ActivityId, inputRequest.OperationalDate, context.Activity.ActivityTypeName,
                context.Field.Name, inputRequest.Status.ToString(), inputRequest.RequiresGrower,
                inputRequest.Version, lineDtos));
        }

        var sessionPerson = membership.PersonId.HasValue
            ? farm.Persons.SingleOrDefault(person => person.Id == membership.PersonId)
            : null;
        return new InputControlWorkspaceDto(
            new TenantSessionDto(tenant.Id, farm.Id, membership.SecurityRole, membership.PersonId, sessionPerson?.DisplayName),
            rules.Select(rule => new InventoryApplicationRuleDto(rule.Id, rule.InventoryItemId,
                rule.ActivityTypeId, rule.EffectiveFrom, rule.EffectiveTo, rule.CoverageBasis.ToString(),
                rule.RatePerCoverageUnit, rule.LowerTolerancePercent, rule.UpperTolerancePercent,
                rule.UnitCodeSnapshot, rule.Version)).ToArray(),
            requestDtos,
            issues.Select(issue => new StockIssueDto(issue.Id, issue.InputRequestId, issue.IssueDate,
                issue.IssuerPersonId, issue.RecipientPersonId, issue.Status.ToString(), issue.PostedAt,
                issue.Version, issue.Lines.Select(line => new StockIssueLineDto(line.Id,
                    line.InputRequestLineId, line.InventoryItemId, line.InventoryLotId,
                    line.ItemCodeSnapshot, line.ItemNameSnapshot, line.LotCodeSnapshot,
                    line.UnitCodeSnapshot, line.Quantity, line.IssueUnitCostUsd, line.IssueValueUsd)).ToArray())).ToArray(),
            items.Select(InventoryMapper.Item).ToArray(), lots.Select(InventoryMapper.Lot).ToArray(),
            tenant.ActivityTypes.Select(type => new ActivityTypeDto(type.Id, type.Code, type.Name,
                type.SupportsPlanned, type.SupportsUnplanned, type.QuantityBasis.ToString(),
                type.Status.ToString(), type.Version)).ToArray(),
            farm.Persons.Select(person => new PersonDto(person.Id, person.DisplayName, person.Phone,
                person.ActiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                person.ActiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), person.Status.ToString(),
                person.Version, person.RoleAssignments.Select(role => new PersonRoleAssignmentDto(role.Id,
                    role.Role.ToString(), role.IsPrimary, role.EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    role.EffectiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).ToArray())).ToArray(),
            invitations.Select(invitation => new ManagerInvitationDto(invitation.Id, invitation.PersonId,
                invitation.ExpiresAt, invitation.RevokedAt, invitation.RedeemedAt, invitation.Version)).ToArray());
    }

    private static (Cane360.Domain.Farms.Field Field, Cane360.Domain.Activities.Activity Activity) FindActivity(
        Farm farm, Guid activityId)
    {
        foreach (var field in farm.Fields)
        foreach (var cycle in field.CropCycles)
        {
            var activity = cycle.Activities.SingleOrDefault(candidate => candidate.Id == activityId);
            if (activity is not null) return (field, activity);
        }
        throw new NotFoundException(activityId.ToString(), "Activity");
    }
}
