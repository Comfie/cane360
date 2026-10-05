namespace Cane360.Application.Inventory;

public sealed class DecideFieldAccountabilityCorrectionCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<DecideFieldAccountabilityCorrectionCommand>
{
    public async Task Handle(DecideFieldAccountabilityCorrectionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        FieldAccountabilityCorrection candidate =
            await inventoryRepository.GetFieldAccountabilityCorrectionAsync(tenant.Id, farm.Id, command.CorrectionId,
                false, cancellationToken)
            ?? throw new NotFoundException(command.CorrectionId.ToString(), "Field-accountability correction");
        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        FieldAccountabilityCorrection correction =
            await inventoryRepository.GetFieldAccountabilityCorrectionAsync(tenant.Id, farm.Id, candidate.Id, true,
                cancellationToken)
            ?? throw new NotFoundException(candidate.Id.ToString(), "Field-accountability correction");
        if (correction.Version != command.ExpectedVersion)
        {
            throw new ConflictException("This correction changed after it was loaded. Refresh and try again.");
        }

        ApprovalDecision? previous =
            await inventoryRepository.GetFieldAccountabilityCorrectionApprovalAsync(correction.Id,
                command.ExpectedVersion, cancellationToken);
        if (previous?.IdempotencyKey == command.IdempotencyKey)
        {
            return;
        }

        if (previous is not null)
        {
            throw new ConflictException("This correction version has already been decided.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion),
            () => correction.Decide(command.Outcome, now, command.ExpectedVersion));
        inventoryRepository.Add(ApprovalDecision.CreateFieldAccountabilityCorrectionDecision(tenant.Id, farm.Id,
            correction.Id,
            command.ExpectedVersion, command.Outcome, userId, TenantSecurityRoles.Grower, now, command.Reason,
            command.IdempotencyKey));
        InventoryAudit.Correction(inventoryRepository, tenant, farm, user, correction, command.Outcome.ToString(), now,
            command.Reason,
            "Grower decided the correction against its exact immutable request version.");
        if (command.Outcome == ApprovalOutcome.Approved)
        {
            await ApplyApprovedCorrectionAsync(correction, tenant, farm, user, inventoryRepository, now,
                cancellationToken);
            InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion),
                () => correction.MarkApplied(now, correction.Version));
            InventoryAudit.Correction(inventoryRepository, tenant, farm, user, correction, "Applied", now,
                command.Reason,
                "Approved correction was applied through append-only reversals and a replacement-ready trace.");
        }

        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ApplyApprovedCorrectionAsync(FieldAccountabilityCorrection correction, Tenant tenant,
        Farm farm,
        IUser user, IInventoryRepository repository, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (correction.FieldReceiptId.HasValue)
        {
            FieldReceipt receipt = await repository.GetFieldReceiptAsync(tenant.Id, farm.Id,
                                       correction.FieldReceiptId.Value, true, cancellationToken)
                                   ?? throw new NotFoundException(correction.FieldReceiptId.Value.ToString(),
                                       "Field receipt");
            if (receipt.Version != correction.SourceVersion || receipt.Status != FieldReceiptStatus.Recorded)
            {
                throw new ConflictException("The field receipt changed after this correction was requested.");
            }

            if (await repository.HasConfirmedApplicationForFieldReceiptAsync(receipt.Id, cancellationToken))
            {
                throw new ConflictException(
                    "A field receipt with confirmed application history cannot be superseded until its application correction is approved.");
            }

            InventoryAccess.ApplyDomainAction(nameof(correction.SourceVersion),
                () => receipt.Supersede(correction.SourceVersion));
            InventoryAudit.FieldReceipt(repository, tenant, farm, user, receipt, "Superseded", now, correction.Reason,
                "Approved correction preserved the original receipt and superseded it for replacement entry.");
        }
        else if (correction.InputApplicationId.HasValue)
        {
            InputApplication application = await repository.GetInputApplicationAsync(tenant.Id, farm.Id,
                                               correction.InputApplicationId.Value, true, cancellationToken)
                                           ?? throw new NotFoundException(
                                               correction.InputApplicationId.Value.ToString(), "Input application");
            if (application.Version != correction.SourceVersion ||
                application.Status != InputApplicationStatus.ManagerConfirmed)
            {
                throw new ConflictException("The application changed after this correction was requested.");
            }

            await repository.LockStockIssueLinesAsync(
                application.Lines.Select(x => x.StockIssueLineId).Order().ToArray(), cancellationToken);
            foreach (InputApplicationLine line in application.Lines)
            foreach (OperationalCostPosting posting in await repository.GetActiveOperationalCostPostingsAsync(line.Id,
                         null, cancellationToken))
            {
                OperationalCostPosting reversal =
                    OperationalCostPosting.Reverse(posting, $"cost:{posting.Id:N}:correction:{correction.Id:N}");
                repository.Add(reversal);
                InventoryAudit.Cost(repository, tenant, farm, user, reversal, "Reversed", now,
                    "Approved application correction appended an exact opposite cost posting.");
            }

            InventoryAccess.ApplyDomainAction(nameof(correction.SourceVersion),
                () => application.Supersede(correction.SourceVersion));
            await repository.SaveChangesAsync(cancellationToken);
            foreach (InputApplicationLine line in application.Lines)
            {
                StockIssueLine issueLine =
                    await repository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true,
                        cancellationToken)
                    ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
                await InventoryAccountability.SynchronizeExceptionAsync(repository, tenant, farm, user,
                    correction.ActivityId, issueLine, now, cancellationToken);
            }

            InventoryAudit.Application(repository, tenant, farm, user, application, "Superseded", now,
                correction.Reason,
                "Approved correction preserved the confirmed application and reopened accountability for its replacement.");
        }
        else if (correction.InventoryLossId.HasValue)
        {
            InventoryLoss loss = await repository.GetInventoryLossAsync(tenant.Id, farm.Id,
                                     correction.InventoryLossId.Value, true, cancellationToken)
                                 ?? throw new NotFoundException(correction.InventoryLossId.Value.ToString(),
                                     "Inventory loss");
            if (loss.Version != correction.SourceVersion || loss.Status != InventoryLossStatus.Approved)
            {
                throw new ConflictException("The approved loss changed after this correction was requested.");
            }

            await repository.LockStockIssueLinesAsync([loss.StockIssueLineId], cancellationToken);
            foreach (OperationalCostPosting posting in await repository.GetActiveOperationalCostPostingsAsync(null,
                         loss.Id, cancellationToken))
            {
                OperationalCostPosting reversal =
                    OperationalCostPosting.Reverse(posting, $"cost:{posting.Id:N}:correction:{correction.Id:N}");
                repository.Add(reversal);
                InventoryAudit.Cost(repository, tenant, farm, user, reversal, "Reversed", now,
                    "Approved loss correction appended an exact opposite loss cost posting.");
            }

            InventoryAccess.ApplyDomainAction(nameof(correction.SourceVersion),
                () => loss.Supersede(correction.SourceVersion));
            await repository.SaveChangesAsync(cancellationToken);
            StockIssueLine issueLine =
                await repository.GetStockIssueLineAsync(tenant.Id, farm.Id, loss.StockIssueLineId, true,
                    cancellationToken)
                ?? throw new NotFoundException(loss.StockIssueLineId.ToString(), "Stock issue line");
            await InventoryAccountability.SynchronizeExceptionAsync(repository, tenant, farm, user,
                correction.ActivityId, issueLine, now, cancellationToken);
            InventoryAudit.Loss(repository, tenant, farm, user, loss, "Superseded", now, correction.Reason,
                "Approved correction preserved the loss and reopened accountability for a replacement decision.");
        }
        else if (correction.StockReturnId.HasValue)
        {
            StockReturn stockReturn = await repository.GetStockReturnAsync(tenant.Id, farm.Id,
                                          correction.StockReturnId.Value, true, cancellationToken)
                                      ?? throw new NotFoundException(correction.StockReturnId.Value.ToString(),
                                          "Stock return");
            if (stockReturn.Version != correction.SourceVersion || stockReturn.Status != StockReturnStatus.Posted)
            {
                throw new ConflictException("The posted return changed after this correction was requested.");
            }

            await repository.LockStoreAsync(tenant.Id, farm.Id, stockReturn.StoreId, cancellationToken);
            await repository.LockStockPositionsAsync(
                stockReturn.Lines.Select(x => x.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
            IReadOnlyList<StockMovement> movements =
                await repository.GetReturnMovementsAsync(stockReturn.Id, cancellationToken);
            if (movements.Count != stockReturn.Lines.Count)
            {
                throw new ConflictException("Return history is incomplete and cannot be corrected.");
            }

            foreach (StockMovement movement in movements)
            {
                StockLedgerSnapshot snapshot =
                    await repository.GetPositionSnapshotAsync(movement.StockPositionId, cancellationToken);
                if (snapshot.Quantity + movement.SignedQuantity < 0 || snapshot.ValueUsd + movement.SignedValueUsd < 0)
                {
                    throw new ConflictException("Return correction would make store stock or value negative.");
                }

                StockReturnLine line = stockReturn.Lines.Single(x => x.Id == movement.StockReturnLineId);
                repository.Add(StockMovement.CreateReturnReversal(movement, stockReturn, line, now,
                    InventoryAccess.RequireUserId(user), $"return:{line.Id:N}:correction:{correction.Id:N}"));
            }

            InventoryAccess.ApplyDomainAction(nameof(correction.SourceVersion), () => stockReturn.MarkReversed(now,
                $"correction:{correction.Id:N}", correction.SourceVersion));
            await repository.SaveChangesAsync(cancellationToken);
            foreach (StockReturnLine line in stockReturn.Lines)
            {
                StockIssueLine issueLine =
                    await repository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true,
                        cancellationToken)
                    ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
                await InventoryAccountability.SynchronizeExceptionAsync(repository, tenant, farm, user,
                    correction.ActivityId, issueLine, now, cancellationToken);
            }

            InventoryAudit.Return(repository, tenant, farm, user, stockReturn, "Reversed", now, correction.Reason,
                "Approved correction appended exact opposite return movements; original return remains visible.");
        }
    }
}
