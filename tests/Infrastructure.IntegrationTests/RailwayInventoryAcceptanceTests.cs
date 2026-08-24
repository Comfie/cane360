using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddInventoryLedgerFoundation is approved and applied to Railway development.")]
[Category("PostMigration")]
[NonParallelizable]
public sealed class RailwayInventoryAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;
    private string _userId = string.Empty;
    private Guid _tenantId;
    private Guid _farmId;
    private Guid _storeId;

    [OneTimeSetUp]
    public async Task EstablishIsolatedSyntheticTenant()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET")
            .ShouldBe("RailwayDevelopment", "acceptance tests require an explicit Railway-development target marker");
        _connectionString = LoadConfiguredConnectionString();
        _runId = $"AUTOTEST-P5A-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        _userId = $"p5a-{Guid.NewGuid():N}";
        var tenant = Tenant.CreateForGrower(_userId, _runId, null);
        var farm = tenant.CreateFarm(
            $"P5A-{Guid.NewGuid():N}"[..20], _runId, "Synthetic acceptance address",
            "Railway development", "Synthetic", 1m, "Synthetic acceptance record");
        _tenantId = tenant.Id;
        _farmId = farm.Id;
        _storeId = farm.Store.Id;
        TestContext.Progress.WriteLine($"Retained synthetic test run: {_runId}; tenant: {_tenantId}");
        await using var context = CreateContext();
        context.Users.Add(new ApplicationUser
        {
            Id = _userId,
            UserName = $"{_runId}@invalid.example",
            NormalizedUserName = $"{_runId}@INVALID.EXAMPLE",
            Email = $"{_runId}@invalid.example",
            NormalizedEmail = $"{_runId}@INVALID.EXAMPLE",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        });
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task AppendOnlyTablesRejectUpdateAndDeleteAtDatabaseBoundary()
    {
        var posted = await CreateAndPostReceiptAsync(7m, 2m);
        var correctionDraft = await CreateReceiptAsync(3m, 2m);
        var correctionPosted = await PostAsync(new PostStockReceiptCommand(
            correctionDraft.ReceiptId, correctionDraft.Version, $"{_runId}-append-post-{Guid.NewGuid():N}"));
        await ReverseAsync(new ReverseStockReceiptCommand(
            correctionDraft.ReceiptId, correctionPosted.Version, "Append-only acceptance reversal",
            $"{_runId}-append-reverse-{Guid.NewGuid():N}"));
        var approvalId = await CreateOpeningApprovalAsync();
        await using var context = CreateContext();
        var correctionId = await context.CorrectionRecords
            .Where(record => record.TenantId == _tenantId && record.OriginalStockReceiptId == correctionDraft.ReceiptId)
            .Select(record => record.Id)
            .SingleAsync();
        var auditId = await context.AuditEvents
            .Where(audit => audit.TenantId == _tenantId && audit.SubjectId == posted.ReceiptId && audit.Action == "Posted")
            .Select(audit => audit.Id)
            .SingleAsync();

        await AssertAppendOnlyAsync(
            $"UPDATE inventory.\"StockMovements\" SET \"SignedQuantity\" = 99 WHERE \"Id\" = {posted.MovementId} AND \"TenantId\" = {_tenantId}");
        await AssertAppendOnlyAsync(
            $"DELETE FROM inventory.\"StockMovements\" WHERE \"Id\" = {posted.MovementId} AND \"TenantId\" = {_tenantId}");
        await AssertAppendOnlyAsync(
            $"UPDATE inventory.\"ApprovalDecisions\" SET \"Reason\" = 'tamper' WHERE \"Id\" = {approvalId} AND \"TenantId\" = {_tenantId}");
        await AssertAppendOnlyAsync(
            $"UPDATE inventory.\"CorrectionRecords\" SET \"Reason\" = 'tamper' WHERE \"Id\" = {correctionId} AND \"TenantId\" = {_tenantId}");
        await AssertAppendOnlyAsync(
            $"UPDATE audit.\"AuditEvents\" SET \"SafeSummary\" = 'tamper' WHERE \"Id\" = {auditId} AND \"TenantId\" = {_tenantId}");
    }

    [Test]
    public async Task RetriedPostingDoesNotDuplicateMovements()
    {
        var draft = await CreateReceiptAsync(4m, 3m);
        var command = new PostStockReceiptCommand(draft.ReceiptId, draft.Version, $"{_runId}-retry-{Guid.NewGuid():N}");
        await PostAsync(command);
        await PostAsync(command);
        await using var context = CreateContext();

        var count = await context.StockMovements.CountAsync(movement =>
            movement.TenantId == _tenantId && movement.StockReceiptLineId == draft.LineId);

        count.ShouldBe(1);
    }

    [Test]
    public async Task MovingAverageUsesPostingOrderWithoutRetroactiveRecosting()
    {
        var item = await CreateInventoryIdentityAsync();
        var first = await CreateReceiptAsync(10m, 2m, item);
        await PostAsync(new PostStockReceiptCommand(first.ReceiptId, first.Version, $"{_runId}-wma-1-{Guid.NewGuid():N}"));
        var backdated = await CreateReceiptAsync(10m, 4m, item, new DateOnly(2026, 1, 1));
        await PostAsync(new PostStockReceiptCommand(backdated.ReceiptId, backdated.Version, $"{_runId}-wma-2-{Guid.NewGuid():N}"));
        await using var context = CreateContext();
        var repository = new InventoryRepository(context);

        var snapshot = await repository.GetPositionSnapshotAsync(item.PositionId, CancellationToken.None);
        var sequences = await context.StockMovements
            .Where(movement => movement.TenantId == _tenantId && movement.InventoryItemId == item.ItemId)
            .OrderBy(movement => movement.PostingSequence)
            .Select(movement => new { movement.PostingSequence, movement.EventDate, movement.SignedValueUsd })
            .ToArrayAsync();

        snapshot.Quantity.ShouldBe(20m);
        snapshot.ValueUsd.ShouldBe(60m);
        snapshot.WeightedAverageUnitCostUsd.ShouldBe(3m);
        sequences[0].SignedValueUsd.ShouldBe(20m);
        sequences[1].EventDate.ShouldBe(new DateOnly(2026, 1, 1));
        sequences[1].PostingSequence.ShouldBeGreaterThan(sequences[0].PostingSequence);
    }

    [Test]
    public async Task ConcurrentReceiptsSerializeThroughStoreAndBothPostOnce()
    {
        var item = await CreateInventoryIdentityAsync();
        var first = await CreateReceiptAsync(6m, 2m, item);
        var second = await CreateReceiptAsync(8m, 3m, item);

        await Task.WhenAll(
            PostAsync(new PostStockReceiptCommand(first.ReceiptId, first.Version, $"{_runId}-concurrent-a-{Guid.NewGuid():N}")),
            PostAsync(new PostStockReceiptCommand(second.ReceiptId, second.Version, $"{_runId}-concurrent-b-{Guid.NewGuid():N}")));
        await using var context = CreateContext();
        var movements = await context.StockMovements
            .Where(movement => movement.TenantId == _tenantId && movement.InventoryItemId == item.ItemId)
            .OrderBy(movement => movement.PostingSequence)
            .ToArrayAsync();

        movements.Length.ShouldBe(2);
        movements.Sum(movement => movement.SignedQuantity).ShouldBe(14m);
        movements[1].PostingSequence.ShouldBeGreaterThan(movements[0].PostingSequence);
    }

    [Test]
    public async Task RepositoryQueriesCannotCrossSyntheticTenantBoundary()
    {
        var ownItem = await CreateInventoryIdentityAsync();
        var otherUserId = $"p5a-{Guid.NewGuid():N}";
        var otherTenant = Tenant.CreateForGrower(otherUserId, $"{_runId}-OTHER", null);
        var otherFarm = otherTenant.CreateFarm(
            $"P5A-{Guid.NewGuid():N}"[..20], $"{_runId}-OTHER", "Synthetic acceptance address",
            "Railway development", "Synthetic", 1m, "Synthetic acceptance record");
        await using (var write = CreateContext())
        {
            write.Users.Add(new ApplicationUser
            {
                Id = otherUserId,
                UserName = $"{otherUserId}@invalid.example",
                NormalizedUserName = $"{otherUserId}@INVALID.EXAMPLE",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            });
            write.Tenants.Add(otherTenant);
            await write.SaveChangesAsync();
        }
        await using var context = CreateContext();
        var repository = new InventoryRepository(context);

        var leaked = await repository.GetItemAsync(
            otherTenant.Id, otherFarm.Id, ownItem.ItemId, false, CancellationToken.None);
        var ownItems = await repository.GetItemsAsync(
            _tenantId, _farmId, false, CancellationToken.None);

        leaked.ShouldBeNull();
        ownItems.ShouldContain(item => item.Id == ownItem.ItemId);
        ownItems.ShouldAllBe(item => item.TenantId == _tenantId && item.FarmId == _farmId);
    }

    [Test]
    public async Task ReversalBlocksDependentHistoryAndPreservesCorrectionLinks()
    {
        var item = await CreateInventoryIdentityAsync();
        var first = await CreateReceiptAsync(5m, 2m, item);
        var firstPosted = await PostAsync(new PostStockReceiptCommand(
            first.ReceiptId, first.Version, $"{_runId}-guard-first-{Guid.NewGuid():N}"));
        var second = await CreateReceiptAsync(5m, 4m, item);
        var secondPosted = await PostAsync(new PostStockReceiptCommand(
            second.ReceiptId, second.Version, $"{_runId}-guard-second-{Guid.NewGuid():N}"));

        await Should.ThrowAsync<ConflictException>(() => ReverseAsync(new ReverseStockReceiptCommand(
            first.ReceiptId, firstPosted.Version, "Dependent history guard",
            $"{_runId}-guard-block-{Guid.NewGuid():N}")));
        var reversed = await ReverseAsync(new ReverseStockReceiptCommand(
            second.ReceiptId, secondPosted.Version, "Authorised latest-receipt correction",
            $"{_runId}-guard-reverse-{Guid.NewGuid():N}"));
        await using var context = CreateContext();
        var correction = await context.CorrectionRecords.SingleAsync(record =>
            record.TenantId == _tenantId && record.OriginalStockReceiptId == second.ReceiptId);

        reversed.Status.ShouldBe(nameof(StockReceiptStatus.Reversed));
        correction.OriginalStockMovementId.ShouldNotBe(Guid.Empty);
        correction.CorrectingStockMovementId.ShouldNotBe(Guid.Empty);
        correction.OriginalStockMovementId.ShouldNotBe(correction.CorrectingStockMovementId);
    }

    private async Task<PostedReceipt> CreateAndPostReceiptAsync(decimal quantity, decimal unitCostUsd)
    {
        var draft = await CreateReceiptAsync(quantity, unitCostUsd);
        await PostAsync(new PostStockReceiptCommand(
            draft.ReceiptId, draft.Version, $"{_runId}-post-{Guid.NewGuid():N}"));
        await using var context = CreateContext();
        var movementId = await context.StockMovements
            .Where(movement => movement.TenantId == _tenantId && movement.StockReceiptLineId == draft.LineId)
            .Select(movement => movement.Id)
            .SingleAsync();
        return new PostedReceipt(draft.ReceiptId, movementId);
    }

    private async Task<Guid> CreateOpeningApprovalAsync()
    {
        var identity = await CreateInventoryIdentityAsync();
        await using var context = CreateContext();
        var item = await context.InventoryItems.SingleAsync(value =>
            value.TenantId == _tenantId && value.FarmId == _farmId && value.Id == identity.ItemId);
        var receipt = StockReceipt.Create(
            _tenantId, _farmId, _storeId, StockReceiptType.OpeningBalance, null,
            new DateOnly(2026, 8, 22), null, $"{_runId}-opening-{Guid.NewGuid():N}",
            "Synthetic opening approval", "Synthetic acceptance entry", 3);
        receipt.AddLine(item, null, 2m, 1m, receipt.Version);
        receipt.SubmitOpeningBalance(receipt.Version);
        var subjectVersion = receipt.Version;
        var approval = ApprovalDecision.CreateOpeningBalanceDecision(
            _tenantId, _farmId, receipt.Id, subjectVersion, ApprovalOutcome.Approved,
            _userId, TenantSecurityRoles.Grower, DateTimeOffset.UtcNow, null,
            $"{_runId}-approval-{Guid.NewGuid():N}");
        receipt.RecordOpeningDecision(ApprovalOutcome.Approved, subjectVersion);
        context.StockReceipts.Add(receipt);
        context.ApprovalDecisions.Add(approval);
        await context.SaveChangesAsync();
        return approval.Id;
    }

    private async Task AssertAppendOnlyAsync(FormattableString sql)
    {
        await using var context = CreateContext();
        var action = async () => await context.Database.ExecuteSqlInterpolatedAsync(sql);
        (await Should.ThrowAsync<PostgresException>(action)).SqlState.ShouldBe(PostgresErrorCodes.RaiseException);
    }

    private async Task<InventoryIdentity> CreateInventoryIdentityAsync()
    {
        var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var unit = UnitOfMeasure.Create(_tenantId, $"U{token}", $"Synthetic unit {_runId}", "Mass", 6);
        var item = InventoryItem.Create(
            _tenantId, _farmId, $"I{token}", $"Synthetic item {_runId}", InventoryItemCategory.Other,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var position = StockPosition.Create(_tenantId, _farmId, _storeId, item.Id, null);
        await using var context = CreateContext();
        context.UnitOfMeasures.Add(unit);
        context.InventoryItems.Add(item);
        context.StockPositions.Add(position);
        await context.SaveChangesAsync();
        return new InventoryIdentity(item.Id, position.Id, unit.Id);
    }

    private async Task<DraftReceipt> CreateReceiptAsync(
        decimal quantity,
        decimal unitCostUsd,
        InventoryIdentity? identity = null,
        DateOnly? receiptDate = null)
    {
        identity ??= await CreateInventoryIdentityAsync();
        await using var context = CreateContext();
        var item = await context.InventoryItems.SingleAsync(value =>
            value.TenantId == _tenantId && value.FarmId == _farmId && value.Id == identity.ItemId);
        var supplier = Supplier.Create(
            _tenantId, _farmId, $"S{Guid.NewGuid():N}"[..20], $"Synthetic supplier {_runId}", null);
        var receipt = StockReceipt.Create(
            _tenantId, _farmId, _storeId, StockReceiptType.Purchase, supplier.Id,
            receiptDate ?? new DateOnly(2026, 8, 22), null, $"{_runId}-{Guid.NewGuid():N}",
            null, "Synthetic acceptance entry", 3);
        var line = receipt.AddLine(item, null, quantity, unitCostUsd, receipt.Version);
        context.Suppliers.Add(supplier);
        context.StockReceipts.Add(receipt);
        await context.SaveChangesAsync();
        return new DraftReceipt(receipt.Id, line.Id, receipt.Version);
    }

    private async Task<StockReceiptDto> PostAsync(PostStockReceiptCommand command)
    {
        await using var context = CreateContext();
        var handler = new PostStockReceiptCommandHandler(
            new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(_userId),
            TimeProvider.System);
        return await handler.Handle(command, CancellationToken.None);
    }

    private async Task<StockReceiptDto> ReverseAsync(ReverseStockReceiptCommand command)
    {
        await using var context = CreateContext();
        var handler = new ReverseStockReceiptCommandHandler(
            new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(_userId),
            TimeProvider.System);
        return await handler.Handle(command, CancellationToken.None);
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string LoadConfiguredConnectionString()
    {
        var environmentValue = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db");
        if (!string.IsNullOrWhiteSpace(environmentValue)) return environmentValue;
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables()
            .Build();
        return configuration.GetConnectionString("Cane360Db")
            ?? throw new InvalidOperationException("The configured Railway development connection is unavailable.");
    }

    private sealed class AcceptanceUser(string userId) : IUser
    {
        public string? Id => userId;
        public List<string>? Roles => null;
        public string? CorrelationId => $"p5a-acceptance-{Guid.NewGuid():N}";
    }

    private sealed record InventoryIdentity(Guid ItemId, Guid PositionId, Guid UnitId);
    private sealed record DraftReceipt(Guid ReceiptId, Guid LineId, long Version);
    private sealed record PostedReceipt(Guid ReceiptId, Guid MovementId);
}
