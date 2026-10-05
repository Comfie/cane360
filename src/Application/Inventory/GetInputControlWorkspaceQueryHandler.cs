using System.Globalization;
using Cane360.Application.Activities;

namespace Cane360.Application.Inventory;

public sealed class GetInputControlWorkspaceQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetInputControlWorkspaceQuery, InputControlWorkspaceDto>
{
    public async Task<InputControlWorkspaceDto> Handle(
        GetInputControlWorkspaceQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        TenantMembership membership =
            tenant.Memberships.Single(item => item.UserId == userId && item.Status == RecordStatus.Active);
        IReadOnlyList<InventoryApplicationRule> rules =
            await inventoryRepository.GetRulesAsync(tenant.Id, farm.Id, cancellationToken);
        IReadOnlyList<InputRequest> requests =
            await inventoryRepository.GetInputRequestsAsync(tenant.Id, farm.Id, request.ActivityId, false,
                cancellationToken);
        IReadOnlyList<StockIssue> issues =
            await inventoryRepository.GetStockIssuesAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        IReadOnlyList<InventoryItem> items =
            await inventoryRepository.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        IReadOnlyList<InventoryLot> lots =
            await inventoryRepository.GetLotsAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        IReadOnlyList<ManagerInvitation> invitations =
            await inventoryRepository.GetManagerInvitationsAsync(tenant.Id, farm.Id, false, cancellationToken);
        IReadOnlyList<FieldReceipt> fieldReceipts =
            await inventoryRepository.GetFieldReceiptsAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        IReadOnlyList<InventoryLoss> losses =
            await inventoryRepository.GetInventoryLossesAsync(tenant.Id, farm.Id, request.ActivityId, false,
                cancellationToken);
        List<InputAccountabilityDto> accountability = new();
        foreach (StockIssue issue in issues.Where(candidate => candidate.Status == StockIssueStatus.Posted))
        {
            InputRequest? issueRequest = requests.SingleOrDefault(candidate => candidate.Id == issue.InputRequestId) ??
                                         await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id,
                                             issue.InputRequestId, false, cancellationToken);
            if (issueRequest is null)
            {
                continue;
            }

            foreach (StockIssueLine line in issue.Lines)
            {
                (decimal Received, decimal Applied, decimal Returned, decimal Loss, decimal Unaccounted) values =
                    await InventoryAccountability.GetAsync(inventoryRepository, line, cancellationToken);
                accountability.Add(new InputAccountabilityDto(issue.Id, line.Id, issueRequest.ActivityId,
                    line.ItemCodeSnapshot, line.LotCodeSnapshot, line.UnitCodeSnapshot, line.Quantity,
                    values.Received, values.Applied, values.Returned, values.Loss, values.Unaccounted,
                    values.Unaccounted != 0));
            }
        }

        List<InputRequestDto> requestDtos = new();
        foreach (InputRequest inputRequest in requests)
        {
            (Field Field, Activity Activity) context = FindActivity(farm, inputRequest.ActivityId);
            List<InputRequestLineDto> lineDtos = new();
            foreach (InputRequestLine line in inputRequest.Lines.OrderBy(candidate => candidate.LineNumber))
            {
                (decimal Quantity, decimal ValueUsd) live =
                    await inventoryRepository.GetItemStockSnapshotAsync(tenant.Id, farm.Id, line.InventoryItemId,
                        cancellationToken);
                decimal issued = await inventoryRepository.GetPostedIssueQuantityAsync(line.Id, cancellationToken);
                lineDtos.Add(new InputRequestLineDto(line.Id, line.InventoryItemId, line.ItemCodeSnapshot,
                    line.ItemNameSnapshot, line.UnitCodeSnapshot, line.InventoryApplicationRuleId,
                    line.RuleVersionSnapshot, line.CoverageBasisSnapshot.ToString(), line.PlannedCoverage,
                    line.PlannedRate, line.PlannedQuantity, line.RequestedQuantity,
                    decimal.Round(line.PlannedQuantity * (1 - (line.LowerTolerancePercent / 100m)), 6),
                    decimal.Round(line.PlannedQuantity * (1 + (line.UpperTolerancePercent / 100m)), 6),
                    line.ApprovalRequirement.ToString(), line.AvailableQuantitySnapshot, live.Quantity,
                    line.EstimatedUnitCostUsdSnapshot, line.EstimatedValueUsdSnapshot, issued,
                    decimal.Max(0, line.RequestedQuantity - issued)));
            }

            requestDtos.Add(new InputRequestDto(inputRequest.Id, inputRequest.FieldId, inputRequest.CropCycleId,
                inputRequest.ActivityId, inputRequest.OperationalDate, context.Activity.ActivityTypeName,
                context.Field.Name, inputRequest.Status.ToString(), inputRequest.RequiresGrower,
                inputRequest.Version, lineDtos));
        }

        Person? sessionPerson = membership.PersonId.HasValue
            ? farm.Persons.SingleOrDefault(person => person.Id == membership.PersonId)
            : null;
        return new InputControlWorkspaceDto(
            new TenantSessionDto(tenant.Id, farm.Id, membership.SecurityRole, membership.PersonId,
                sessionPerson?.DisplayName),
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
                        line.UnitCodeSnapshot, line.Quantity, line.IssueUnitCostUsd, line.IssueValueUsd)).ToArray()))
                .ToArray(),
            items.Select(InventoryMapper.Item).ToArray(), lots.Select(InventoryMapper.Lot).ToArray(),
            tenant.ActivityTypes.Select(type => new ActivityTypeDto(type.Id, type.Code, type.Name,
                type.SupportsPlanned, type.SupportsUnplanned, type.QuantityBasis.ToString(),
                type.Status.ToString(), type.Version)).ToArray(),
            farm.Persons.Select(person => new PersonDto(person.Id, person.DisplayName, person.Phone,
                person.ActiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                person.ActiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), person.Status.ToString(),
                person.Version, person.RoleAssignments.Select(role => new PersonRoleAssignmentDto(role.Id,
                    role.Role.ToString(), role.IsPrimary,
                    role.EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    role.EffectiveTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).ToArray())).ToArray(),
            invitations.Select(invitation => new ManagerInvitationDto(invitation.Id, invitation.PersonId,
                invitation.ExpiresAt, invitation.RevokedAt, invitation.RedeemedAt, invitation.Version,
                invitation.SecurityRole)).ToArray(),
            fieldReceipts.Select(receipt => new FieldReceiptDto(receipt.Id, receipt.StockIssueId, receipt.FieldId,
                receipt.CropCycleId, receipt.ActivityId, receipt.RecipientPersonId, receipt.ReceivedAt,
                receipt.Status.ToString(), receipt.Version, receipt.Lines.Select(line => new FieldReceiptLineDto(
                    line.Id, line.StockIssueLineId, line.ItemCodeSnapshot, line.LotCodeSnapshot,
                    line.UnitCodeSnapshot, line.Quantity)).ToArray())).ToArray(),
            losses.Select(loss => new InventoryLossDto(loss.Id, loss.ActivityId, loss.StockIssueLineId,
                loss.ItemCodeSnapshot, loss.LotCodeSnapshot, loss.UnitCodeSnapshot, loss.Quantity,
                loss.LossType.ToString(), loss.Reason, loss.Status.ToString(), loss.Version)).ToArray(),
            accountability);
    }

    private static (Field Field, Activity Activity) FindActivity(
        Farm farm, Guid activityId)
    {
        foreach (Field field in farm.Fields)
        foreach (CropCycle cycle in field.CropCycles)
        {
            Activity? activity = cycle.Activities.SingleOrDefault(candidate => candidate.Id == activityId);
            if (activity is not null)
            {
                return (field, activity);
            }
        }

        throw new NotFoundException(activityId.ToString(), "Activity");
    }
}
