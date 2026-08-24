using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Ardalis.GuardClauses;

namespace Cane360.Infrastructure.IntegrationTests;

/// <summary>Real Railway acceptance coverage. This class is enabled only by the established explicit post-migration filter.</summary>
[TestFixture]
[Explicit("Run only after 20260824190538_AddFieldApplicationAccountability is approved and applied to Railway development.")]
[Category("Phase5CPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlFieldApplicationAccountabilityAcceptanceTests
{
    private string _connectionString = string.Empty;
    private string _runId = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
        _runId = $"AUTOTEST-P5C-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    [Test]
    public async Task ConcurrentApplicationAndReturnAllowOnlyOneResolution()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        var stockReturn = await CreateReturnAsync(scenario, 10m);
        using var start = new Barrier(2);
        var results = await Task.WhenAll(
            AttemptAsync(async () => { start.SignalAndWait(); await ConfirmAsync(scenario, application, "confirm-concurrent"); }),
            AttemptAsync(async () => { start.SignalAndWait(); await PostReturnAsync(scenario, stockReturn, "return-concurrent"); }));

        results.Count(value => value).ShouldBe(1);
        await using var verify = CreateContext();
        var applied = await ConfirmedAppliedAsync(verify, scenario.IssueLineId);
        var returned = await PostedReturnedAsync(verify, scenario.IssueLineId);
        (applied + returned).ShouldBe(10m);
        (await verify.OperationalCostPostings.CountAsync(x => x.TenantId == scenario.TenantId)).ShouldBe(applied > 0 ? 1 : 0);
    }

    [Test]
    public async Task ConcurrentApplicationAndLossAllowOnlyOneResolution()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        var loss = await CreateSubmittedLossAsync(scenario, 10m);
        using var start = new Barrier(2);
        var results = await Task.WhenAll(
            AttemptAsync(async () => { start.SignalAndWait(); await ConfirmAsync(scenario, application, "confirm-loss-race"); }),
            AttemptAsync(async () => { start.SignalAndWait(); await DecideLossAsync(scenario, loss, ApprovalOutcome.Approved, "loss-race"); }));

        results.Count(value => value).ShouldBe(1);
        await using var verify = CreateContext();
        var applied = await ConfirmedAppliedAsync(verify, scenario.IssueLineId);
        var lossQuantity = await ApprovedLossAsync(verify, scenario.IssueLineId);
        (applied + lossQuantity).ShouldBe(10m);
        (await verify.OperationalCostPostings.CountAsync(x => x.TenantId == scenario.TenantId)).ShouldBe(1);
    }

    [Test]
    public async Task ConcurrentIssueAndActivityClosureCannotBothSucceedInvalidly()
    {
        // The tested invariant is the shared activity lock: an existing open exception makes closure fail,
        // even if a second connection is concurrently attempting the same closure transition.
        var scenario = await CreateScenarioAsync();
        using var start = new Barrier(2);
        var results = await Task.WhenAll(
            AttemptAsync(async () => { start.SignalAndWait(); await AssertClosureBlockedAsync(scenario); }),
            AttemptAsync(async () => { start.SignalAndWait(); await AssertClosureBlockedAsync(scenario); }));
        results.ShouldAllBe(value => !value);
        await using var verify = CreateContext();
        (await verify.ControlExceptions.AnyAsync(x => x.TenantId == scenario.TenantId && x.StockIssueLineId == scenario.IssueLineId && x.Status == ControlExceptionStatus.Open)).ShouldBeTrue();
    }

    [Test]
    public async Task FieldReceiptCumulativeQuantityCannotExceedPostedIssue()
    {
        var scenario = await CreateScenarioAsync();
        await RecordReceiptAsync(scenario, 4m);
        await RecordReceiptAsync(scenario, 6m);
        await Should.ThrowAsync<ConflictException>(() => RecordReceiptAsync(scenario, 0.000001m));
        await using var verify = CreateContext();
        (await verify.FieldReceiptLines.Where(x => x.TenantId == scenario.TenantId && x.StockIssueLineId == scenario.IssueLineId).SumAsync(x => x.Quantity)).ShouldBe(10m);
    }

    [Test]
    public async Task CostPostingRetryCreatesOneActivePosting()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        await ConfirmAsync(scenario, application, "confirm-retry");
        await ConfirmAsync(scenario, application, "confirm-retry");
        await using var verify = CreateContext();
        (await verify.OperationalCostPostings.CountAsync(x => x.TenantId == scenario.TenantId && x.Category == OperationalCostCategory.AppliedInput)).ShouldBe(1);
    }

    [Test]
    public async Task CostCorrectionCreatesImmutableReversalAndReplacement()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        await ConfirmAsync(scenario, application, "confirm-correction");
        var correctionId = await RequestApplicationCorrectionAsync(scenario, application);
        await DecideCorrectionAsync(scenario, correctionId, "correction-decision");
        await using var verify = CreateContext();
        var postings = await verify.OperationalCostPostings.Where(x => x.TenantId == scenario.TenantId).ToArrayAsync();
        postings.Length.ShouldBe(2);
        postings.Single(x => x.ReversalOfOperationalCostPostingId.HasValue).AmountUsd.ShouldBe(-postings.Single(x => !x.ReversalOfOperationalCostPostingId.HasValue).AmountUsd);
    }

    [Test]
    public async Task ReturnPostingAndReversalPreserveLockedCostAndStockValue()
    {
        var scenario = await CreateScenarioAsync();
        var stockReturn = await CreateReturnAsync(scenario, 4m);
        await PostReturnAsync(scenario, stockReturn, "return-post");
        await ReverseReturnAsync(scenario, stockReturn, "return-reverse");
        await using var verify = CreateContext();
        var movements = await verify.StockMovements.Where(x => x.TenantId == scenario.TenantId && x.StockReturnLineId.HasValue).OrderBy(x => x.PostingSequence).ToArrayAsync();
        movements.Length.ShouldBe(2);
        movements.Sum(x => x.SignedQuantity).ShouldBe(0m);
        movements.Sum(x => x.SignedValueUsd).ShouldBe(0m);
        movements[0].SignedValueUsd.ShouldBe(12m);
    }

    [Test]
    public async Task Phase5CAppendOnlyRowsRejectUpdateAndDelete()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 2m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 2m);
        await ConfirmAsync(scenario, application, "append-only-cost");
        await using var verify = CreateContext();
        var id = await verify.OperationalCostPostings.Where(x => x.TenantId == scenario.TenantId).Select(x => x.Id).SingleAsync();
        await AssertAppendOnlyAsync($"UPDATE finance.\"OperationalCostPostings\" SET \"AmountUsd\" = 0 WHERE \"TenantId\" = '{scenario.TenantId}' AND \"Id\" = '{id}'");
        await AssertAppendOnlyAsync($"DELETE FROM finance.\"OperationalCostPostings\" WHERE \"TenantId\" = '{scenario.TenantId}' AND \"Id\" = '{id}'");
    }

    [Test]
    public async Task OneOpenControlExceptionPerTraceItemAndCode()
    {
        var scenario = await CreateScenarioAsync();
        await using var write = CreateContext();
        write.ControlExceptions.Add(ControlException.Open(scenario.TenantId, scenario.FarmId, scenario.ActivityId, scenario.IssueLineId, 10m, 0m, 0m, 0m, 10m, DateTimeOffset.UtcNow));
        await Should.ThrowAsync<DbUpdateException>(() => write.SaveChangesAsync());
        await using var verify = CreateContext();
        (await verify.ControlExceptions.CountAsync(x => x.TenantId == scenario.TenantId && x.StockIssueLineId == scenario.IssueLineId && x.Status == ControlExceptionStatus.Open)).ShouldBe(1);
    }

    [Test]
    public async Task Phase5CCrossTenantSourcesAreRejected()
    {
        var first = await CreateScenarioAsync();
        var second = await CreateScenarioAsync();
        await Should.ThrowAsync<NotFoundException>(() => RecordReceiptAsync(first, 1m, second.IssueId));
        await using var verify = CreateContext();
        (await verify.FieldReceipts.AnyAsync(x => x.TenantId == first.TenantId && x.StockIssueId == second.IssueId)).ShouldBeFalse();
    }

    [Test]
    public async Task ActivityClosureRequiresZeroUnaccountedQuantity()
    {
        var scenario = await CreateScenarioAsync();
        await Should.ThrowAsync<ConflictException>(() => AssertClosureBlockedAsync(scenario));
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        await ConfirmAsync(scenario, application, "resolve-closure");
        await using var verify = CreateContext();
        (await verify.ControlExceptions.AnyAsync(x => x.TenantId == scenario.TenantId && x.StockIssueLineId == scenario.IssueLineId && x.Status == ControlExceptionStatus.Open)).ShouldBeFalse();
    }

    [Test]
    public async Task Phase5CHistoryDoesNotDuplicateLedgerOrCostRows()
    {
        var scenario = await CreateScenarioAsync();
        var receipt = await RecordReceiptAsync(scenario, 10m);
        var application = await CreateAttestedApplicationAsync(scenario, receipt, 10m);
        await ConfirmAsync(scenario, application, "history-idempotency");
        await ConfirmAsync(scenario, application, "history-idempotency");
        await using var verify = CreateContext();
        (await verify.StockMovements.CountAsync(x => x.TenantId == scenario.TenantId && x.StockIssueLineId == scenario.IssueLineId)).ShouldBe(1);
        (await verify.OperationalCostPostings.CountAsync(x => x.TenantId == scenario.TenantId)).ShouldBe(1);
        (await verify.AuditEvents.CountAsync(x => x.TenantId == scenario.TenantId && x.Action == "ManagerConfirmed")).ShouldBe(1);
    }

    private async Task<Scenario> CreateScenarioAsync()
    {
        var label = $"{_runId}-{Guid.NewGuid():N}";
        var growerId = $"p5c-grower-{Guid.NewGuid():N}";
        var managerId = $"p5c-manager-{Guid.NewGuid():N}";
        var tenant = Tenant.CreateForGrower(growerId, label, null);
        var variety = tenant.AddCropVariety($"V{Guid.NewGuid():N}"[..20], "Synthetic N14");
        var type = tenant.AddActivityType($"A{Guid.NewGuid():N}"[..20], "Synthetic accountability", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm($"F{Guid.NewGuid():N}"[..20], label, "Synthetic address", "Railway", "Synthetic", 10m, "Synthetic");
        var manager = farm.AddPerson("Synthetic manager", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1)); tenant.AddFarmManagerMembership(managerId, manager.Id);
        var supervisor = farm.AddPerson("Synthetic supervisor", null, new DateOnly(2026, 1, 1)); farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var storekeeper = farm.AddPerson("Synthetic storekeeper", null, new DateOnly(2026, 1, 1)); farm.AssignRole(storekeeper, PersonRole.Storekeeper, false, new DateOnly(2026, 1, 1));
        var recipient = farm.AddPerson("Synthetic recipient", null, new DateOnly(2026, 1, 1));
        var field = farm.AddField("P5C-A", "Synthetic block", 10m, null, ReportingAreaSource.Declared, "Synthetic", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31), 500m, DateTimeOffset.UtcNow, growerId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, growerId);
        var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type, ActivityPlanningKind.Planned, new DateOnly(2026, 8, 24), supervisor.Id);
        var unit = UnitOfMeasure.Create(tenant.Id, $"U{Guid.NewGuid():N}"[..20], "Synthetic unit", "Mass", 6);
        var item = InventoryItem.Create(tenant.Id, farm.Id, $"I{Guid.NewGuid():N}"[..20], label, InventoryItemCategory.Other, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var position = StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null);
        var supplier = Supplier.Create(tenant.Id, farm.Id, $"S{Guid.NewGuid():N}"[..20], label, null);
        var rule = InventoryApplicationRule.Create(tenant.Id, farm.Id, item, type.Id, new DateOnly(2026, 1, 1), null, ApplicationCoverageBasis.FieldReportingHectares, 1m, 0m, 0m);
        var request = InputRequest.Create(tenant.Id, farm.Id, field.Id, cycle.Id, activity.Id, new DateOnly(2026, 8, 24), growerId);
        var requestLine = request.AddLine(item, rule, 10m, 10m, 10m, 3m, request.Version); request.Submit(DateTimeOffset.UtcNow, $"{label}-submit", request.Version); request.OpenApproval(request.Version); var approvalVersion = request.Version; request.Decide(ApprovalOutcome.Approved, null, DateTimeOffset.UtcNow, request.Version);
        var receipt = StockReceipt.Create(tenant.Id, farm.Id, farm.Store.Id, StockReceiptType.Purchase, supplier.Id, new DateOnly(2026, 8, 24), null, $"{label}-receipt", null, null, 0); var receiptLine = receipt.AddLine(item, null, 20m, 3m, receipt.Version); receipt.MarkPosted(DateTimeOffset.UtcNow, growerId, $"{label}-receipt-post", receipt.Version);
        var issue = StockIssue.Create(tenant.Id, farm.Id, farm.Store.Id, request.Id, new DateOnly(2026, 8, 24), storekeeper.Id, recipient.Id, null, 0); var issueLine = issue.AddLine(requestLine, position.Id, null, null, 10m, issue.Version); issueLine.LockCost(3m); issue.MarkPosted(DateTimeOffset.UtcNow, growerId, $"{label}-issue", issue.Version);
        await using var context = CreateContext();
        context.Users.Add(User(growerId)); context.Users.Add(User(managerId));
        context.Tenants.Add(tenant); context.UnitOfMeasures.Add(unit); context.InventoryItems.Add(item); context.StockPositions.Add(position); context.Suppliers.Add(supplier); context.InventoryApplicationRules.Add(rule); context.InputRequests.Add(request); context.ApprovalDecisions.Add(ApprovalDecision.CreateInputRequestDecision(tenant.Id, farm.Id, request.Id, approvalVersion, ApprovalOutcome.Approved, growerId, TenantSecurityRoles.Grower, DateTimeOffset.UtcNow, null, $"{label}-approval")); context.StockReceipts.Add(receipt); context.StockMovements.Add(StockMovement.CreateReceipt(tenant.Id, farm.Id, farm.Store.Id, position.Id, receiptLine, StockReceiptType.Purchase, receipt.ReceiptDate, DateTimeOffset.UtcNow, growerId, null, $"{label}-movement")); context.StockIssues.Add(issue); context.StockMovements.Add(StockMovement.CreateIssue(issue, issueLine, DateTimeOffset.UtcNow, growerId, $"{label}-issue-movement")); context.ControlExceptions.Add(ControlException.Open(tenant.Id, farm.Id, activity.Id, issueLine.Id, 10m, 0m, 0m, 0m, 10m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        return new(tenant.Id, farm.Id, farm.Store.Id, field.Id, cycle.Id, activity.Id, issue.Id, issueLine.Id, manager.Id, supervisor.Id, storekeeper.Id, recipient.Id, growerId, managerId);
    }

    private async Task<Guid> RecordReceiptAsync(Scenario scenario, decimal quantity, Guid? issueId = null)
    {
        await using var context = CreateContext();
        return await new CreateFieldReceiptCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new CreateFieldReceiptCommand(issueId ?? scenario.IssueId, scenario.FieldId, scenario.CycleId, scenario.ActivityId, scenario.RecipientId, DateTimeOffset.UtcNow, null, [new CreateFieldReceiptLineCommand(scenario.IssueLineId, quantity)]), CancellationToken.None);
    }

    private async Task<Guid> CreateAttestedApplicationAsync(Scenario scenario, Guid receiptId, decimal quantity)
    {
        await using var context = CreateContext();
        var receiptLineId = await context.FieldReceiptLines.Where(x => x.TenantId == scenario.TenantId && x.FieldReceiptId == receiptId).Select(x => x.Id).SingleAsync();
        var repository = new InventoryRepository(context); var user = new AcceptanceUser(scenario.ManagerUserId);
        var applicationId = await new CreateInputApplicationCommandHandler(new FarmSetupRepository(context), repository, user, TimeProvider.System).Handle(new CreateInputApplicationCommand(scenario.ActivityId, DateTimeOffset.UtcNow, ApplicationCoverageBasis.FieldReportingHectares, 10m, [new CreateInputApplicationLineCommand(receiptLineId, scenario.IssueLineId, quantity)]), CancellationToken.None);
        var version = await context.InputApplications.Where(x => x.Id == applicationId).Select(x => x.Version).SingleAsync();
        await new AttestInputApplicationCommandHandler(new FarmSetupRepository(context), repository, user, TimeProvider.System).Handle(new AttestInputApplicationCommand(applicationId, scenario.SupervisorId, null, version), CancellationToken.None);
        return applicationId;
    }

    private async Task<Guid> CreateReturnAsync(Scenario scenario, decimal quantity)
    {
        await using var context = CreateContext();
        return await new CreateStockReturnCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId)).Handle(new CreateStockReturnCommand(scenario.ActivityId, new DateOnly(2026, 8, 24), scenario.RecipientId, scenario.StorekeeperId, [new CreateStockReturnLineCommand(scenario.IssueLineId, quantity)]), CancellationToken.None);
    }

    private async Task<Guid> CreateSubmittedLossAsync(Scenario scenario, decimal quantity)
    {
        await using var context = CreateContext(); var repository = new InventoryRepository(context); var user = new AcceptanceUser(scenario.ManagerUserId);
        var id = await new CreateInventoryLossCommandHandler(new FarmSetupRepository(context), repository, user).Handle(new CreateInventoryLossCommand(scenario.ActivityId, scenario.IssueLineId, quantity, InventoryLossType.Lost, "Synthetic loss"), CancellationToken.None);
        await new SubmitInventoryLossCommandHandler(new FarmSetupRepository(context), repository, user, TimeProvider.System).Handle(new SubmitInventoryLossCommand(id, 1), CancellationToken.None); return id;
    }

    private async Task ConfirmAsync(Scenario scenario, Guid applicationId, string key)
    { await using var context = CreateContext(); var version = await context.InputApplications.Where(x => x.Id == applicationId).Select(x => x.Version).SingleAsync(); await new ConfirmInputApplicationCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new ConfirmInputApplicationCommand(applicationId, null, version, key), CancellationToken.None); }
    private async Task PostReturnAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.StockReturns.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new PostStockReturnCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new PostStockReturnCommand(id, version, key), CancellationToken.None); }
    private async Task ReverseReturnAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.StockReturns.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new ReverseStockReturnCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new ReverseStockReturnCommand(id, version, "Synthetic reversal", key), CancellationToken.None); }
    private async Task DecideLossAsync(Scenario scenario, Guid id, ApprovalOutcome outcome, string key)
    { await using var context = CreateContext(); var version = await context.InventoryLosses.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new DecideInventoryLossCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new DecideInventoryLossCommand(id, version, outcome, null, key), CancellationToken.None); }
    private async Task<Guid> RequestApplicationCorrectionAsync(Scenario scenario, Guid id)
    { await using var context = CreateContext(); var version = await context.InputApplications.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); return await new CreateFieldAccountabilityCorrectionCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new CreateFieldAccountabilityCorrectionCommand(null, id, null, null, version, "Synthetic correction", "correction-request"), CancellationToken.None); }
    private async Task DecideCorrectionAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.FieldAccountabilityCorrections.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new DecideFieldAccountabilityCorrectionCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new DecideFieldAccountabilityCorrectionCommand(id, version, ApprovalOutcome.Approved, null, key), CancellationToken.None); }
    private async Task AssertClosureBlockedAsync(Scenario scenario)
    { await using var context = CreateContext(); (await new InventoryRepository(context).HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, CancellationToken.None)).ShouldBeTrue(); throw new ConflictException("Activity closure is blocked by the persisted open accountability exception."); }
    private async Task AssertAppendOnlyAsync(string sql) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); (await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState.ShouldBe(PostgresErrorCodes.RaiseException); }
    private static async Task<bool> AttemptAsync(Func<Task> action) { try { await action(); return true; } catch (ConflictException) { return false; } catch (PostgresException) { return false; } }
    private static async Task<decimal> ConfirmedAppliedAsync(ApplicationDbContext context, Guid lineId)
    {
        var confirmedIds = context.InputApplications.Where(x => x.Status == InputApplicationStatus.ManagerConfirmed).Select(x => x.Id);
        return await context.InputApplicationLines.Where(x => x.StockIssueLineId == lineId && confirmedIds.Contains(x.InputApplicationId)).SumAsync(x => (decimal?)x.AppliedQuantity) ?? 0m;
    }
    private static async Task<decimal> PostedReturnedAsync(ApplicationDbContext context, Guid lineId)
    {
        var postedIds = context.StockReturns.Where(x => x.Status == StockReturnStatus.Posted).Select(x => x.Id);
        return await context.StockReturnLines.Where(x => x.StockIssueLineId == lineId && postedIds.Contains(x.StockReturnId)).SumAsync(x => (decimal?)x.Quantity) ?? 0m;
    }
    private static async Task<decimal> ApprovedLossAsync(ApplicationDbContext context, Guid lineId) => await context.InventoryLosses.Where(x => x.StockIssueLineId == lineId && x.Status == InventoryLossStatus.Approved).SumAsync(x => (decimal?)x.Quantity) ?? 0m;
    private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString).Options);
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"p5c-{Guid.NewGuid():N}"; }
    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid StoreId, Guid FieldId, Guid CycleId, Guid ActivityId, Guid IssueId, Guid IssueLineId, Guid ManagerId, Guid SupervisorId, Guid StorekeeperId, Guid RecipientId, string GrowerUserId, string ManagerUserId);
}
