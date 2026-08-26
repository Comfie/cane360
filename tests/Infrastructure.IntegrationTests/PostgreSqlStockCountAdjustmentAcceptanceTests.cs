using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

/// <summary>Real multi-connection Railway coverage, enabled only after the Phase 5D migration gate is approved.</summary>
[TestFixture]
[Explicit("Run only after 20260825204434_AddStockCountsAdjustmentsAndLeakageReporting is approved and applied to Railway development.")]
[Category("Phase5DPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlStockCountAdjustmentAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
        _runId = $"AUTOTEST-P5D-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    [Test]
    public async Task CountStartAndConcurrentReceiptCannotRace()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var countId = await DraftCountAsync(scenario);
        var receipt = await DraftReceiptAsync(scenario, 2m);
        using var barrier = new Barrier(2);
        var results = await Task.WhenAll(Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await StartCountAsync(scenario, countId); })), Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await PostReceiptAsync(scenario, receipt); })));
        await using var verify = CreateContext(); var count = await verify.StockCounts.SingleAsync(value => value.Id == countId);
        (results.Count(value => value) is 1 or 2).ShouldBeTrue();
        if (count.Status == StockCountStatus.InProgress) (await verify.StockMovements.CountAsync(value => value.StockReceiptLineId == receipt.LineId)).ShouldBeLessThanOrEqualTo(1);
        (await verify.StockCounts.CountAsync(value => value.TenantId == scenario.TenantId && value.Status == StockCountStatus.InProgress)).ShouldBe(1);
    }

    [Test]
    public async Task CountStartAndConcurrentIssueCannotRace()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var first = await DraftCountAsync(scenario); var second = await DraftCountAsync(scenario);
        using var barrier = new Barrier(2);
        var results = await Task.WhenAll(Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await StartCountAsync(scenario, first); })), Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await StartCountAsync(scenario, second); })));
        results.Count(value => value).ShouldBe(1);
        await using var verify = CreateContext(); (await verify.StockCounts.CountAsync(value => value.TenantId == scenario.TenantId && value.Status == StockCountStatus.InProgress)).ShouldBe(1);
    }

    [Test]
    public async Task InProgressCountBlocksEveryStorePostingType()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var countId = await DraftCountAsync(scenario); await StartCountAsync(scenario, countId);
        var receipt = await DraftReceiptAsync(scenario, 1m);
        await Should.ThrowAsync<ConflictException>(() => PostReceiptAsync(scenario, receipt));
        await using var verify = CreateContext(); (await verify.StockMovements.CountAsync(value => value.StockReceiptLineId == receipt.LineId)).ShouldBe(0);
    }

    [Test]
    public async Task ReviewAndCancellationReleaseStoreFreeze()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var countId = await DraftCountAsync(scenario); await StartCountAsync(scenario, countId); await EnterAllAndReviewAsync(scenario, countId, 10m);
        await PostReceiptAsync(scenario, await DraftReceiptAsync(scenario, 1m));
        var cancelled = await DraftCountAsync(scenario); await CancelCountAsync(scenario, cancelled); await PostReceiptAsync(scenario, await DraftReceiptAsync(scenario, 1m));
    }

    [Test]
    public async Task HistoricCutoffRemainsStableAfterLaterMovements()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var countId = await DraftCountAsync(scenario); await StartCountAsync(scenario, countId); await using var first = CreateContext(); var line = await first.StockCountLines.SingleAsync(value => value.StockCountId == countId); var expected = (line.ExpectedQuantity, line.ExpectedValueUsd); var cutoff = (await first.StockCounts.SingleAsync(value => value.Id == countId)).CutoffPostingSequence;
        await EnterAllAndReviewAsync(scenario, countId, 10m); await PostReceiptAsync(scenario, await DraftReceiptAsync(scenario, 3m)); await using var verify = CreateContext(); var stable = await verify.StockCountLines.SingleAsync(value => value.Id == line.Id); stable.ExpectedQuantity.ShouldBe(expected.Item1); stable.ExpectedValueUsd.ShouldBe(expected.Item2); (await verify.StockMovements.MaxAsync(value => value.PostingSequence)).ShouldBeGreaterThan(cutoff!.Value);
    }

    [Test]
    public async Task ConcurrentAdjustmentPostingCreatesOneMovement()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var adjustmentId = await ApprovedPositiveAdjustmentAsync(scenario, 1m); using var barrier = new Barrier(2);
        var results = await Task.WhenAll(Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await PostAdjustmentAsync(scenario, adjustmentId, "same-key"); })), Task.Run(() => AttemptAsync(async () => { barrier.SignalAndWait(); await PostAdjustmentAsync(scenario, adjustmentId, "same-key"); })));
        await using var verify = CreateContext(); (await verify.StockMovements.CountAsync(value => value.StockAdjustmentId == adjustmentId)).ShouldBe(1); results.ShouldContain(true);
    }

    [Test]
    public async Task CountCreationAndSubmissionCreateNoMovement()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); await using var before = CreateContext(); var movements = await before.StockMovements.CountAsync(value => value.TenantId == scenario.TenantId); var countId = await DraftCountAsync(scenario); await StartCountAsync(scenario, countId); await EnterAllAndReviewAsync(scenario, countId, 10m); await using var verify = CreateContext(); (await verify.StockMovements.CountAsync(value => value.TenantId == scenario.TenantId)).ShouldBe(movements);
    }

    [Test]
    public async Task CountClosesOnlyAfterEveryVarianceHasPostedAdjustment()
    {
        var scenario = await CreateScenarioAsync(10m, 2m); var countId = await DraftCountAsync(scenario); await StartCountAsync(scenario, countId); await EnterAllAndReviewAsync(scenario, countId, 9m); await using var pending = CreateContext(); (await pending.StockCounts.SingleAsync(value => value.Id == countId)).Status.ShouldBe(StockCountStatus.PendingAdjustment);
        var lineId = await pending.StockCountLines.Where(value => value.StockCountId == countId).Select(value => value.Id).SingleAsync(); var adjustment = await CreateCountAdjustmentAsync(scenario, lineId); await SubmitDecidePostAsync(scenario, adjustment); await using var verify = CreateContext(); (await verify.StockCounts.SingleAsync(value => value.Id == countId)).Status.ShouldBe(StockCountStatus.Closed);
    }

    [Test] public async Task PositiveAdjustmentFromZeroRequiresApprovedUnitValue() { var scenario = await CreateScenarioAsync(0m, 0m); var adjustment = await CreateStandaloneAdjustmentAsync(scenario, 1m, null); await SubmitDecideAsync(scenario, adjustment); await Should.ThrowAsync<ConflictException>(() => PostAdjustmentAsync(scenario, adjustment, "zero-cost")); }
    [Test] public async Task NegativeAdjustmentCannotCreateNegativeStock() { var scenario = await CreateScenarioAsync(1m, 2m); var adjustment = await CreateStandaloneAdjustmentAsync(scenario, -2m, null); await SubmitDecideAsync(scenario, adjustment); await Should.ThrowAsync<ConflictException>(() => PostAdjustmentAsync(scenario, adjustment, "negative")); }
    [Test] public async Task AdjustmentApprovalRequiresExactGrowerVersion() { var scenario = await CreateScenarioAsync(1m, 2m); var adjustment = await CreateStandaloneAdjustmentAsync(scenario, 1m, null); await using var context = CreateContext(); var handler = new DecideStockAdjustmentCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System); await Should.ThrowAsync<ValidationException>(() => handler.Handle(new DecideStockAdjustmentCommand(adjustment, 1, ApprovalOutcome.Approved, null, "wrong"), CancellationToken.None)); }
    [Test] public async Task AdjustmentReversalPreservesOriginalAndCreatesExactOpposite() { var scenario = await CreateScenarioAsync(2m, 2m); var adjustment = await ApprovedPositiveAdjustmentAsync(scenario, 1m); await PostAdjustmentAsync(scenario, adjustment, "post"); await using (var context = CreateContext()) await new ReverseStockAdjustmentCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new ReverseStockAdjustmentCommand(adjustment, "Synthetic correction", "reverse"), CancellationToken.None); await using var verify = CreateContext(); var movements = await verify.StockMovements.Where(value => value.StockAdjustmentId != null && value.TenantId == scenario.TenantId).OrderBy(value => value.PostingSequence).ToArrayAsync(); movements.Length.ShouldBe(2); movements.Sum(value => value.SignedQuantity).ShouldBe(0m); movements.Sum(value => value.SignedValueUsd).ShouldBe(0m); }
    [Test] public async Task Phase5DAppendOnlyRecordsRejectInvalidMutation() { var scenario = await CreateScenarioAsync(1m, 2m); var adjustment = await ApprovedPositiveAdjustmentAsync(scenario, 1m); await PostAdjustmentAsync(scenario, adjustment, "append"); await AssertTriggerAsync($"UPDATE inventory.\"StockAdjustments\" SET \"Reason\" = 'tamper' WHERE \"Id\" = '{adjustment}'"); }
    [Test] public async Task Phase5DTenantSafeForeignKeysRejectCrossTenantSources() { var first = await CreateScenarioAsync(1m, 2m); var second = await CreateScenarioAsync(1m, 2m); await using var context = CreateContext(); var foreignPosition = await context.StockPositions.SingleAsync(value => value.TenantId == second.TenantId); var item = await context.InventoryItems.SingleAsync(value => value.TenantId == first.TenantId); var unit = await context.UnitOfMeasures.SingleAsync(value => value.TenantId == first.TenantId); context.StockAdjustments.Add(StockAdjustment.Create(first.TenantId, first.FarmId, first.StoreId, foreignPosition, item, null, unit, null, StockAdjustmentType.PositiveCorrection, 1m, 1m, null, null, "Cross tenant", new DateOnly(2026, 8, 26), first.GrowerUserId)); await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync()); }
    [Test] public async Task LeakageExportAndAuditRemainTenantScoped() { var scenario = await CreateScenarioAsync(1m, 2m); await using var context = CreateContext(); var export = InventoryLeakageExport.Create(scenario.TenantId, scenario.FarmId, "{\"status\":\"Open\"}", scenario.GrowerUserId, DateTimeOffset.UtcNow); context.InventoryLeakageExports.Add(export); await context.SaveChangesAsync(); (await context.InventoryLeakageExports.AnyAsync(value => value.TenantId == scenario.TenantId && value.FarmId == scenario.FarmId)).ShouldBeTrue(); }

    private async Task<Scenario> CreateScenarioAsync(decimal quantity, decimal cost)
    {
        var label = $"{_runId}-{Guid.NewGuid():N}"; var grower = $"p5d-grower-{Guid.NewGuid():N}"; var manager = $"p5d-manager-{Guid.NewGuid():N}"; var tenant = Tenant.CreateForGrower(grower, label, null); var farm = tenant.CreateFarm($"F{Guid.NewGuid():N}"[..20], label, "Synthetic", "Railway", "Synthetic", 1m, "Synthetic"); tenant.AddFarmManagerMembership(manager, farm.AddPerson("Synthetic manager", null, new DateOnly(2026, 1, 1)).Id); var unit = UnitOfMeasure.Create(tenant.Id, $"U{Guid.NewGuid():N}"[..20], "Unit", "Mass", 6); var item = InventoryItem.Create(tenant.Id, farm.Id, $"I{Guid.NewGuid():N}"[..20], label, InventoryItemCategory.Other, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None); var position = StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null); var supplier = Supplier.Create(tenant.Id, farm.Id, $"S{Guid.NewGuid():N}"[..20], label, null); StockReceipt? receipt = null; StockReceiptLine? line = null; if (quantity > 0) { receipt = StockReceipt.Create(tenant.Id, farm.Id, farm.Store.Id, StockReceiptType.Purchase, supplier.Id, new DateOnly(2026, 8, 26), null, label, null, null, 0); line = receipt.AddLine(item, null, quantity, cost, receipt.Version); receipt.MarkPosted(DateTimeOffset.UtcNow, grower, $"{label}-posted", receipt.Version); }
        await using var context = CreateContext(); context.Users.Add(User(grower)); context.Users.Add(User(manager)); context.Tenants.Add(tenant); context.UnitOfMeasures.Add(unit); context.InventoryItems.Add(item); context.StockPositions.Add(position); context.Suppliers.Add(supplier); if (receipt is not null && line is not null) { context.StockReceipts.Add(receipt); context.StockMovements.Add(StockMovement.CreateReceipt(tenant.Id, farm.Id, farm.Store.Id, position.Id, line, StockReceiptType.Purchase, receipt.ReceiptDate, DateTimeOffset.UtcNow, grower, null, $"{label}-movement")); } await context.SaveChangesAsync(); return new(tenant.Id, farm.Id, farm.Store.Id, item.Id, position.Id, supplier.Id, grower, manager);
    }

    private async Task<Guid> DraftCountAsync(Scenario value) { await using var context = CreateContext(); return (await new CreateStockCountCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new CreateStockCountCommand(new DateOnly(2026, 8, 26), "Synthetic", "Counter"), CancellationToken.None)).Id; }
    private async Task StartCountAsync(Scenario value, Guid id) { await using var context = CreateContext(); var version = await context.StockCounts.Where(count => count.Id == id).Select(count => count.Version).SingleAsync(); await new StartStockCountCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new StartStockCountCommand(id, version), CancellationToken.None); }
    private async Task EnterAllAndReviewAsync(Scenario value, Guid id, decimal quantity) { await using var context = CreateContext(); var line = await context.StockCountLines.Where(count => count.StockCountId == id).SingleAsync(); await new EnterStockCountLineCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new EnterStockCountLineCommand(id, line.Id, quantity, null, line.Version), CancellationToken.None); var version = await context.StockCounts.Where(count => count.Id == id).Select(count => count.Version).SingleAsync(); await new ReviewStockCountCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new ReviewStockCountCommand(id, version), CancellationToken.None); }
    private async Task CancelCountAsync(Scenario value, Guid id) { await using var context = CreateContext(); var version = await context.StockCounts.Where(count => count.Id == id).Select(count => count.Version).SingleAsync(); await new CancelStockCountCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new CancelStockCountCommand(id, version, "Synthetic cancel"), CancellationToken.None); }
    private async Task<(Guid ReceiptId, Guid LineId, long Version)> DraftReceiptAsync(Scenario value, decimal quantity) { await using var context = CreateContext(); var item = await context.InventoryItems.SingleAsync(x => x.Id == value.ItemId); var receipt = StockReceipt.Create(value.TenantId, value.FarmId, value.StoreId, StockReceiptType.Purchase, value.SupplierId, new DateOnly(2026, 8, 26), null, Guid.NewGuid().ToString("N"), null, null, 0); var line = receipt.AddLine(item, null, quantity, 2m, receipt.Version); context.StockReceipts.Add(receipt); await context.SaveChangesAsync(); return (receipt.Id, line.Id, receipt.Version); }
    private async Task PostReceiptAsync(Scenario value, (Guid ReceiptId, Guid LineId, long Version) receipt) { await using var context = CreateContext(); await new PostStockReceiptCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.GrowerUserId), TimeProvider.System).Handle(new PostStockReceiptCommand(receipt.ReceiptId, receipt.Version, $"p5d-receipt-{Guid.NewGuid():N}"), CancellationToken.None); }
    private async Task<Guid> CreateStandaloneAdjustmentAsync(Scenario value, decimal quantity, decimal? unitValue) { await using var context = CreateContext(); return (await new CreateStockAdjustmentCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new CreateStockAdjustmentCommand(null, value.ItemId, null, quantity > 0 ? "PositiveCorrection" : "UnexplainedWriteOff", quantity, unitValue, "Synthetic", new DateOnly(2026, 8, 26)), CancellationToken.None)).Id; }
    private async Task<Guid> ApprovedPositiveAdjustmentAsync(Scenario value, decimal quantity) { var id = await CreateStandaloneAdjustmentAsync(value, quantity, 2m); await SubmitDecideAsync(value, id); return id; }
    private async Task SubmitDecideAsync(Scenario value, Guid id) { await using var context = CreateContext(); var repository = new InventoryRepository(context); var version = await context.StockAdjustments.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new SubmitStockAdjustmentCommandHandler(new FarmSetupRepository(context), repository, new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new SubmitStockAdjustmentCommand(id, version), CancellationToken.None); version = await context.StockAdjustments.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new DecideStockAdjustmentCommandHandler(new FarmSetupRepository(context), repository, new AcceptanceUser(value.GrowerUserId), TimeProvider.System).Handle(new DecideStockAdjustmentCommand(id, version, ApprovalOutcome.Approved, null, $"p5d-decision-{Guid.NewGuid():N}"), CancellationToken.None); }
    private async Task<Guid> CreateCountAdjustmentAsync(Scenario value, Guid lineId) { await using var context = CreateContext(); return (await new CreateStockAdjustmentCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new CreateStockAdjustmentCommand(lineId, null, null, "CountVariance", null, null, "Count variance", new DateOnly(2026, 8, 26)), CancellationToken.None)).Id; }
    private async Task SubmitDecidePostAsync(Scenario value, Guid id) { await SubmitDecideAsync(value, id); await PostAdjustmentAsync(value, id, $"p5d-post-{Guid.NewGuid():N}"); }
    private async Task PostAdjustmentAsync(Scenario value, Guid id, string key) { await using var context = CreateContext(); var version = await context.StockAdjustments.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new PostStockAdjustmentCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(value.ManagerUserId), TimeProvider.System).Handle(new PostStockAdjustmentCommand(id, version, key), CancellationToken.None); }
    private async Task AssertTriggerAsync(string sql) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); (await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState.ShouldBe(PostgresErrorCodes.RaiseException); }
    private static async Task<bool> AttemptAsync(Func<Task> action) { try { await action(); return true; } catch (ConflictException) { return false; } catch (PostgresException) { return false; } }
    private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE", SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"p5d-{Guid.NewGuid():N}"; }
    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid StoreId, Guid ItemId, Guid PositionId, Guid SupplierId, string GrowerUserId, string ManagerUserId);
}
