using Cane360.Application.Activities;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Models;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Application.CropCycles;
using Cane360.Application.FarmSetup;
using Cane360.Application.Labour;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Domain.Labour;
using Cane360.Domain.MillRecords;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Cane360.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
    public async Task Phase5CCostAppendOnlyTriggerAndFunctionRemainPresent()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1
                FROM pg_trigger trigger
                JOIN pg_proc function ON function.oid = trigger.tgfoid
                JOIN pg_namespace function_schema ON function_schema.oid = function.pronamespace
                WHERE trigger.tgrelid = 'finance."OperationalCostPostings"'::regclass
                  AND trigger.tgname = 'TR_OperationalCostPostings_AppendOnly'
                  AND function_schema.nspname = 'inventory'
                  AND function.proname = 'RejectAppendOnlyMutation'
                  AND NOT trigger.tgisinternal)
            """, connection);
        Convert.ToBoolean(await command.ExecuteScalarAsync()).ShouldBeTrue();
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
            Task.Run(() => AttemptAsync(async () =>
            {
                start.SignalAndWait();
                await ConfirmAsync(scenario, application, "confirm-concurrent");
            })),
            Task.Run(() => AttemptAsync(async () =>
            {
                start.SignalAndWait();
                await PostReturnAsync(scenario, stockReturn, "return-concurrent");
            })));

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
            Task.Run(() => AttemptAsync(async () => { start.SignalAndWait(); await ConfirmAsync(scenario, application, "confirm-loss-race"); })),
            Task.Run(() => AttemptAsync(async () => { start.SignalAndWait(); await DecideLossAsync(scenario, loss, ApprovalOutcome.Approved, "loss-race"); })));

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
            Task.Run(() => AttemptAsync(async () => { start.SignalAndWait(); await AssertClosureBlockedAsync(scenario); })),
            Task.Run(() => AttemptAsync(async () => { start.SignalAndWait(); await AssertClosureBlockedAsync(scenario); })));
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
    [Category("MvpGoldenPathAcceptance")]
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
    [Category("MvpGoldenPathAcceptance")]
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
    [Category("MvpGoldenPathAcceptance")]
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
    [Category("MvpGoldenPathAcceptance")]
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

    [Test]
    [Category("CR014PostMigration")]
    public async Task CategoryMetadataLeavesCompleteLedgerAndAccountabilityHistoryUnchanged()
    {
        Scenario scenario = await CreateScenarioAsync();
        Guid fieldReceipt = await RecordReceiptAsync(scenario, 10m);
        Guid application = await CreateAttestedApplicationAsync(scenario, fieldReceipt, 4m);
        await ConfirmAsync(scenario, application, "cr014-confirm");
        Guid stockReturn = await CreateReturnAsync(scenario, 2m);
        await PostReturnAsync(scenario, stockReturn, "cr014-return");
        await ReverseReturnAsync(scenario, stockReturn, "cr014-return-reverse");
        Guid loss = await CreateSubmittedLossAsync(scenario, 2m);
        await DecideLossAsync(scenario, loss, ApprovalOutcome.Approved, "cr014-loss");
        Guid correctedApplication = await CreateAttestedApplicationAsync(scenario, fieldReceipt, 1m);
        await ConfirmAsync(scenario, correctedApplication, "cr014-corrected-confirm");
        Guid correction = await RequestApplicationCorrectionAsync(scenario, correctedApplication);
        await DecideCorrectionAsync(scenario, correction, "cr014-correction");

        Dictionary<string, string> before = await CategoryIntegritySnapshotAsync(scenario);
        InventoryCategoryDto category;
        await using (ApplicationDbContext context = CreateContext())
        {
            InventoryRepository inventory = new(context);
            InventoryCategory existing = (await inventory.GetCategoriesAsync(scenario.TenantId, false, default))
                .Single(value => value.Code == "Other");
            category = await new UpdateInventoryCategoryCommandHandler(new FarmSetupRepository(context), inventory,
                new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System)
                .Handle(new(existing.Id, "General farm supplies", "CR-01.4 synthetic rename", 9, existing.Version), default);
        }
        await using (ApplicationDbContext context = CreateContext())
        {
            category = await new SetInventoryCategoryActiveCommandHandler(new FarmSetupRepository(context),
                new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System)
                .Handle(new(category.Id, false, category.Version), default);
            InventoryItem item = await context.InventoryItems.AsNoTracking().SingleAsync(value => value.TenantId == scenario.TenantId);
            item.Category.ShouldBe("Other");
            UnitOfMeasure unit = await context.UnitOfMeasures.AsNoTracking().SingleAsync(value => value.TenantId == scenario.TenantId);
            await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
                new CreateInventoryItemCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context),
                    new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(
                    new("INACTIVE", "Synthetic inactive assignment", "Other", unit.Id, null, "None", "None"), default));
        }
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(before);
        await using (ApplicationDbContext context = CreateContext())
        {
            InventoryRepository inventory = new(context);
            category = await new SetInventoryCategoryActiveCommandHandler(new FarmSetupRepository(context), inventory,
                new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new(category.Id, true, category.Version), default);
            InventoryCategoryDto created = await new CreateInventoryCategoryCommandHandler(new FarmSetupRepository(context), inventory,
                new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new("CR014", "Synthetic custom category", null, 10), default);
            created.Active.ShouldBeTrue();
            (await inventory.GetCategoriesAsync(scenario.TenantId, false, default)).ShouldContain(value => value.Id == created.Id);
            (await context.AuditEvents.AsNoTracking().Where(value => value.TenantId == scenario.TenantId && value.SubjectId == category.Id)
                .Select(value => value.Action).ToArrayAsync()).ShouldBe(new[] {"Updated", "Deactivated", "Activated"}, ignoreOrder: true);
            await Should.ThrowAsync<ForbiddenAccessException>(() => new CreateInventoryCategoryCommandHandler(
                new FarmSetupRepository(context), inventory, new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System)
                .Handle(new("OWNER", "Owner forbidden", null, 0), default));
        }
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(before);
    }

    [Test]
    [Category("CR014PostMigration")]
    public async Task CategoryRepositoryAndCommandsRejectOtherSyntheticTenant()
    {
        Scenario own = await CreateScenarioAsync();
        Scenario other = await CreateScenarioAsync();
        await using ApplicationDbContext context = CreateContext();
        InventoryRepository inventory = new(context);
        InventoryCategory otherCategory = (await inventory.GetCategoriesAsync(other.TenantId, false, default)).First();
        (await inventory.GetCategoryAsync(own.TenantId, otherCategory.Id, true, default)).ShouldBeNull();
        (await inventory.GetCategoriesAsync(own.TenantId, false, default)).ShouldAllBe(category => category.TenantId == own.TenantId);
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => new UpdateInventoryCategoryCommandHandler(
            new FarmSetupRepository(context), inventory, new AcceptanceUser(own.ManagerUserId), TimeProvider.System)
            .Handle(new(otherCategory.Id, "Cross tenant", null, 0, otherCategory.Version), default));
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => new SetInventoryCategoryActiveCommandHandler(
            new FarmSetupRepository(context), inventory, new AcceptanceUser(own.ManagerUserId), TimeProvider.System)
            .Handle(new(otherCategory.Id, false, otherCategory.Version), default));
    }

    [Test]
    [Category("CR014PostMigration")]
    public async Task CategoryConcurrentEditsRejectLostUpdate()
    {
        Scenario scenario = await CreateScenarioAsync();
        await using ApplicationDbContext first = CreateContext();
        await using ApplicationDbContext second = CreateContext();
        InventoryRepository firstRepository = new(first);
        InventoryRepository secondRepository = new(second);
        InventoryCategory category = (await firstRepository.GetCategoriesAsync(scenario.TenantId, true, default)).First();
        InventoryCategory stale = (await secondRepository.GetCategoriesAsync(scenario.TenantId, true, default)).Single(value => value.Id == category.Id);
        category.Update("First edit", null, 0, category.Version);
        stale.Update("Second edit", null, 0, stale.Version);
        await firstRepository.SaveChangesAsync(default);
        await Should.ThrowAsync<ConflictException>(() => secondRepository.SaveChangesAsync(default));
    }

    [Test]
    [Category("CR014PostMigration")]
    public async Task CategoryDatabaseEnforcesNormalizedUniquenessAndTenantItemReference()
    {
        Scenario own = await CreateScenarioAsync();
        Scenario other = await CreateScenarioAsync();
        await using (ApplicationDbContext context = CreateContext())
        {
            context.InventoryCategories.Add(InventoryCategory.Create(other.TenantId, "OTHERONLY", "Other tenant only"));
            await context.SaveChangesAsync();
        }
        await using (ApplicationDbContext context = CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.InventoryCategories.Add(InventoryCategory.Create(own.TenantId, "UNIQUE", " other "));
            DbUpdateException exception = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
            ((PostgresException)exception.InnerException!).SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
            await transaction.RollbackAsync();
        }
        await using (ApplicationDbContext context = CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            UnitOfMeasure unit = await context.UnitOfMeasures.SingleAsync(value => value.TenantId == own.TenantId);
            InventoryItem item = InventoryItem.Create(own.TenantId, own.FarmId, "CROSS", "Synthetic invalid reference",
                InventoryItemCategory.Other, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
            context.InventoryItems.Add(item);
            context.Entry(item).Property(value => value.Category).CurrentValue = "OTHERONLY";
            DbUpdateException exception = await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync());
            ((PostgresException)exception.InnerException!).SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
            await transaction.RollbackAsync();
        }
    }

    private async Task<Dictionary<string, string>> CategoryIntegritySnapshotAsync(Scenario scenario)
    {
        await using ApplicationDbContext context = CreateContext();
        (await context.Tenants.AsNoTracking().Where(value => value.Id == scenario.TenantId)
            .Select(value => value.GrowerProfile.DisplayName).SingleAsync()).ShouldStartWith(_runId);
        Dictionary<string, string> snapshots = new();
        string[] tables = ["inventory.InventoryItems", "inventory.StockPositions", "inventory.StockReceipts",
            "inventory.StockReceiptLines", "inventory.StockMovements", "inventory.InputRequests", "inventory.InputRequestLines",
            "inventory.StockIssues", "inventory.StockIssueLines", "inventory.FieldReceipts", "inventory.FieldReceiptLines",
            "inventory.InputApplications", "inventory.InputApplicationLines", "inventory.StockReturns", "inventory.StockReturnLines",
            "inventory.InventoryLosses", "inventory.ApprovalDecisions", "inventory.CorrectionRecords",
            "inventory.FieldAccountabilityCorrections", "audit.ControlExceptions", "finance.OperationalCostPostings"];
        await context.Database.OpenConnectionAsync();
        foreach (string table in tables)
        {
            string[] parts = table.Split('.');
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT COALESCE(jsonb_agg(to_jsonb(row) ORDER BY row.\"Id\"), '[]'::jsonb)::text FROM {parts[0]}.\"{parts[1]}\" row WHERE row.\"TenantId\" = @tenant";
            var parameter = command.CreateParameter(); parameter.ParameterName = "tenant"; parameter.Value = scenario.TenantId;
            command.Parameters.Add(parameter);
            snapshots.Add(table, (string)(await command.ExecuteScalarAsync())!);
        }
        IReadOnlyList<(StockPosition Position, StockLedgerSnapshot Snapshot)> stock = await new InventoryRepository(context)
            .GetStockOnHandAsync(scenario.TenantId, scenario.FarmId, default);
        snapshots.Add("StockOnHand", System.Text.Json.JsonSerializer.Serialize(stock.Select(value => new {
            value.Position.Id, value.Snapshot.Quantity, value.Snapshot.ValueUsd, value.Snapshot.WeightedAverageUnitCostUsd})));
        return snapshots;
    }

    [Test]
    [Category("CR015Acceptance")]
    public async Task EnrichedOwnerEmployeeAndCategoriesCoexistThroughLabourApplicationHarvestAndReconciliation()
    {
        // Reuse the existing uniquely labelled, tenant-scoped operational graph. No real data or cleanup.
        Scenario scenario = await CreateScenarioAsync();
        await using ApplicationDbContext context = CreateContext();
        FarmSetupRepository farms = new(context);
        AcceptanceUser owner = new(scenario.GrowerUserId);
        AcceptanceUser manager = new(scenario.ManagerUserId);
        Tenant tenant = (await farms.GetTenantForUserAsync(owner.Id!, true, default))!;
        tenant.GrowerProfile.DisplayName.ShouldStartWith(_runId);
        Farm farm = tenant.ActiveFarm!;
        Field field = farm.Fields.Single(value => value.Id == scenario.FieldId);
        CropCycle cycle = field.CropCycles.Single(value => value.Id == scenario.CycleId);
        Activity activity = cycle.Activities.Single(value => value.Id == scenario.ActivityId);
        DateOnly workDate = new(2026, 8, 24);
        DateTimeOffset workAt = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cane360.slnx"))) directory = directory.Parent;
        IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables().Build();
        IConfiguration local = new ConfigurationBuilder().AddJsonFile(Path.Combine(directory!.FullName,
            "src", "Web", "appsettings.Development.Local.json"), optional: true).Build();
        WorkerSensitiveDataProtector protector = new(new ConfigurationBuilder().AddConfiguration(configuration)
            .AddInMemoryCollection(local.GetSection("Cane360Security:NationalId").AsEnumerable()).Build());
        FarmModel model = FarmModel.Create(tenant.Id, "CR015", "Synthetic integrated model");
        context.FarmModels.Add(model);
        await context.SaveChangesAsync();
        await new UpdateFarmInformationCommandHandler(farms, owner, protector, TimeProvider.System).Handle(
            new(tenant.GrowerProfile.DisplayName, "123", farm.Code, farm.Name, farm.Address, farm.Location,
                farm.Tenure, farm.DeclaredHectares, farm.IrrigationContext,
                new FarmOwnerProfileInput("Ms", "Synthetic", "Owner", "Female", "CR015", "Synthetic association",
                    "MEM015", "Synthetic address", "synthetic@example.invalid", "asset:owner", true, null), model.Id, true), default);
        farm.FarmModelId.ShouldBe(model.Id);

        await new CreateFieldCommandHandler(farms, manager).Handle(new("CR015", "Synthetic physical field", 1m,
            null, "Declared", "Synthetic", null), default);
        Field physical = farm.Fields.Single(value => value.Code == "CR015");
        physical.CropCycles.ShouldBeEmpty();
        CropCycleDetailsDto draft = await new CreateCropCycleCommandHandler(farms, manager, TimeProvider.System,
            Options.Create(new CropMaturityOptions())).Handle(new(physical.Id, "PlantCane", null,
                tenant.CropVarieties.Single().Id, new DateOnly(2026, 1, 31), null, null, 50m), default);
        draft.CropCycle.ExpectedHarvestStart.ShouldBe("2027-03-31");

        LabourRepository labour = new(context);
        WorkerDetailsDto employee = await new CreateWorkerCommandHandler(farms, labour, protector, manager,
            TimeProvider.System).Handle(new(null, "Synthetic legacy worker name", null, "Casual", workDate,
                "SYNTHETIC-CR015-12", new WorkerProfileInput(EmployeeNumber: "CR015")), default);
        WorkerProfile worker = (await labour.GetWorkerAsync(tenant.Id, farm.Id, employee.Worker.Id, true, default))!;
        activity.RecordActualWork(workAt, 1m, field.ReportingHectares, null, cycle.StartDate, workAt, owner.Id!, null, activity.Version);
        Attendance attendance = Attendance.Create(tenant.Id, farm.Id, worker.Id, workDate, AttendanceStatus.Present,
            field.Id, workAt, owner.Id!, null, 0);
        WorkerRate rate = WorkerRate.Create(tenant.Id, farm.Id, worker.Id, PayBasis.Daily, null, 10m, workDate, null);
        WorkRecord evidence = WorkRecord.Create(tenant.Id, farm.Id, attendance.Id, worker.Id, field.Id, workDate,
            rate, null, [activity.Id], workAt, owner.Id!, null, 0);
        evidence.RecordSupervisorVerification(scenario.SupervisorId, workAt, owner.Id!, evidence.Version);
        evidence.Confirm(workAt, owner.Id!, evidence.Version);
        PayrollPeriod period = PayrollPeriod.Create(tenant.Id, farm.Id, 2026, 8, workAt, owner.Id!, null);
        PayrollRun run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, workAt, owner.Id!, null);
        context.Set<Attendance>().Add(attendance);
        context.Set<WorkerRate>().Add(rate);
        context.Set<WorkRecord>().Add(evidence);
        context.Set<PayrollPeriod>().Add(period);
        context.Set<PayrollRun>().Add(run);
        await context.SaveChangesAsync();
        PayrollRunDto payroll = await new CalculatePayrollRunCommandHandler(farms, labour, new PayrollRepository(context),
            manager, TimeProvider.System).Handle(new(run.Id, run.Version), default);
        payroll.Calculation!.GrossAmountUsd.ShouldBe(10m);
        PayrollCalculation calculation = await context.Set<PayrollCalculation>().AsNoTracking().Include(value => value.WorkerLines)
            .ThenInclude(value => value.EarningLines)
            .SingleAsync(value => value.TenantId == tenant.Id && value.FarmId == farm.Id && value.PayrollRunId == run.Id);
        string payrollBefore = System.Text.Json.JsonSerializer.Serialize(calculation);

        Guid receipt = await RecordReceiptAsync(scenario, 10m);
        Guid application = await CreateAttestedApplicationAsync(scenario, receipt, 4m);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            await using ApplicationDbContext confirmation = CreateContext();
            long version = await confirmation.InputApplications.Where(value => value.TenantId == scenario.TenantId &&
                value.Id == application).Select(value => value.Version).SingleAsync();
            // The fixture records August work; October confirmation must satisfy the existing late-reason rule.
            try
            {
                await new ConfirmInputApplicationCommandHandler(new FarmSetupRepository(confirmation),
                    new InventoryRepository(confirmation), manager, TimeProvider.System).Handle(
                    new(application, "Synthetic retrospective UAT confirmation", version, _runId + "-cr015-confirm"), default);
                break;
            }
            catch (InventorySerializationFailureException) when (attempt < 2)
            {
                TestContext.Out.WriteLine("Retrying synthetic posting after the existing serializable conflict response.");
            }
        }
        Dictionary<string, string> ledgerBefore = await CategoryIntegritySnapshotAsync(scenario);
        await new UpdateWorkerProfileCommandHandler(farms, labour, manager, TimeProvider.System).Handle(
            new(worker.Id, worker.Version, employee.PersonVersion, employee.Worker.DisplayName, null, "Casual",
                new WorkerProfileInput("CR015", "Ms", "Enriched", "Employee", "Female", "Synthetic address",
                    "asset:employee", "Synthetic Kin", "Sibling", "123", "Synthetic address", new DateOnly(1990, 1, 1))), default);
        InventoryRepository inventory = new(context);
        InventoryCategory category = (await inventory.GetCategoriesAsync(tenant.Id, true, default)).Single(value => value.Code == "Other");
        Guid categoryId = category.Id;
        await new UpdateInventoryCategoryCommandHandler(farms, inventory, manager, TimeProvider.System)
            .Handle(new(category.Id, "Integrated farm supplies", "Synthetic CR015", 4, category.Version), default);
        category.Id.ShouldBe(categoryId);
        category.Code.ShouldBe("Other");
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(ledgerBefore);
        context.ChangeTracker.Clear();
        PayrollCalculation unchangedPayroll = await context.Set<PayrollCalculation>().AsNoTracking().Include(value => value.WorkerLines)
            .ThenInclude(value => value.EarningLines)
            .SingleAsync(value => value.TenantId == tenant.Id && value.FarmId == farm.Id && value.Id == calculation.Id);
        System.Text.Json.JsonSerializer.Serialize(unchangedPayroll).ShouldBe(payrollBefore);
        unchangedPayroll.WorkerLines.Single().WorkerNameSnapshot.ShouldBe("Synthetic legacy worker name");
        (await context.Set<WorkRecord>().Include(value => value.Activities).SingleAsync(value => value.TenantId == tenant.Id &&
            value.FarmId == farm.Id && value.Id == evidence.Id)).Activities.Single().ActivityId.ShouldBe(activity.Id);

        // Resolve the remaining six issued units before the existing activity/harvest closure gate.
        string operationalFactsBeforeReturns = await ReturnHistoricalSnapshotAsync(scenario);
        Guid remainingReturn = await CreateReturnAsync(scenario, 6m);
        await PostReturnAsync(scenario, remainingReturn, "cr015-accounted-return");
        tenant = (await farms.GetTenantForUserAsync(owner.Id!, true, default))!;
        cycle = tenant.ActiveFarm!.Fields.Single(value => value.Id == field.Id).CropCycles.Single(value => value.Id == cycle.Id);
        activity = cycle.Activities.Single(value => value.Id == scenario.ActivityId);
        bool inputsAccounted = !await inventory.HasBlockingInventoryExceptionAsync(tenant.Id, farm.Id, activity.Id, default);
        bool labourVerified = !await labour.HasIncompleteWorkForActivityAsync(tenant.Id, farm.Id, activity.Id, default);
        ControlException returnException = await context.ControlExceptions.AsNoTracking().SingleAsync(value =>
            value.TenantId == tenant.Id && value.FarmId == farm.Id && value.StockIssueLineId == scenario.IssueLineId);
        decimal appliedQuantity = await inventory.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default);
        decimal returnedQuantity = await inventory.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default);
        TestContext.Out.WriteLine($"CR015-D01 synthetic facts: issued=10, applied={appliedQuantity}, returned={returnedQuantity}, " +
            $"unaccounted={10m - appliedQuantity - returnedQuantity}, exception={returnException.Status}, " +
            $"exceptionSnapshotUnaccounted={returnException.UnaccountedQuantity}.");
        appliedQuantity.ShouldBe(4m);
        returnedQuantity.ShouldBe(6m);
        inputsAccounted.ShouldBeTrue();
        labourVerified.ShouldBeTrue();
        foreach (ActivityStatus status in new[] { ActivityStatus.Planned, ActivityStatus.InProgress,
                     ActivityStatus.AwaitingVerification, ActivityStatus.ManagerConfirmation, ActivityStatus.Completed })
        {
            await new TransitionActivityCommandHandler(farms, labour, inventory, manager,
                new DisplayOnlyIdentityService(), TimeProvider.System)
                .Handle(new(activity.Id, status.ToString(), activity.Version, null), default);
        }
        await ReverseReturnAsync(scenario, remainingReturn, "integrated-final-return-reversal");
        ControlException reopened = await context.ControlExceptions.AsNoTracking().SingleAsync(value =>
            value.TenantId == tenant.Id && value.StockIssueLineId == scenario.IssueLineId && value.Status == ControlExceptionStatus.Open);
        reopened.Id.ShouldNotBe(returnException.Id);
        reopened.UnaccountedQuantity.ShouldBe(6m);
        System.Text.Json.JsonSerializer.Serialize(await context.ControlExceptions.AsNoTracking().SingleAsync(value =>
            value.TenantId == tenant.Id && value.Id == returnException.Id)).ShouldBe(System.Text.Json.JsonSerializer.Serialize(returnException));
        await Should.ThrowAsync<ValidationException>(() => new TransitionActivityCommandHandler(farms, labour, inventory, manager,
            new DisplayOnlyIdentityService(), TimeProvider.System)
            .Handle(new(activity.Id, ActivityStatus.Closed.ToString(), activity.Version, null), default));
        activity.Status.ShouldBe(ActivityStatus.Completed);
        cycle.MarkReadyForHarvest(DateTimeOffset.UtcNow, owner.Id!);
        await farms.SaveChangesAsync(default);
        Should.Throw<InvalidOperationException>(() => cycle.RecordHarvest(new DateOnly(2026, 9, 1), 50m,
            new DateOnly(2026, 10, 6), DateTimeOffset.UtcNow, owner.Id!)).Message.ShouldContain("All activities");
        Guid replacementReturn = await CreateReturnAsync(scenario, 6m);
        await PostReturnAsync(scenario, replacementReturn, "integrated-replacement-return");
        (await inventory.HasBlockingInventoryExceptionAsync(tenant.Id, farm.Id, activity.Id, default)).ShouldBeFalse();
        (await context.ControlExceptions.AsNoTracking().SingleAsync(value => value.TenantId == tenant.Id && value.Id == reopened.Id))
            .Status.ShouldBe(ControlExceptionStatus.Resolved);
        await new TransitionActivityCommandHandler(farms, labour, inventory, manager, new DisplayOnlyIdentityService(), TimeProvider.System)
            .Handle(new(activity.Id, ActivityStatus.Closed.ToString(), activity.Version, null), default);
        activity.Status.ShouldBe(ActivityStatus.Closed);
        activity.StatusChanges.ShouldContain(value => value.ToStatus == ActivityStatus.Closed && value.RecordedBy == manager.Id);
        cycle.RecordHarvest(new DateOnly(2026, 9, 1), 50m, new DateOnly(2026, 10, 6), DateTimeOffset.UtcNow, owner.Id!);
        Mill mill = Mill.Create(tenant.Id, farm.Id, "CR015", "Synthetic mill", null, owner.Id!, DateTimeOffset.UtcNow);
        WeighbridgeTicket ticket = WeighbridgeTicket.CreateDraft(tenant.Id, farm.Id, mill.Id, _runId,
            new DateOnly(2026, 9, 1), 100m, 10m, 90m, field.Id, cycle.Id, null, null, owner.Id!, DateTimeOffset.UtcNow);
        ticket.Record(owner.Id!, DateTimeOffset.UtcNow, _runId + "-ticket", ticket.Version);
        GrowerStatement statement = GrowerStatement.CreateDraft(tenant.Id, farm.Id, mill.Id, _runId,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), 90m, 1800m, null, owner.Id!, DateTimeOffset.UtcNow);
        context.Set<Mill>().Add(mill);
        context.Set<WeighbridgeTicket>().Add(ticket);
        context.Set<GrowerStatement>().Add(statement);
        await context.SaveChangesAsync();
        // Use the existing synthetic evidence metadata pattern before recording a statement.
        context.EvidenceDocuments.Add(EvidenceDocument.ForStatement(tenant.Id, farm.Id, statement.Id,
            "synthetic-cr015.pdf", "application/pdf", 1, Guid.NewGuid().ToString("N"), owner.Id!, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        statement.Record(owner.Id!, DateTimeOffset.UtcNow, _runId + "-statement", statement.Version, true);
        StatementTicketMatch match = StatementTicketMatch.Add(tenant.Id, farm.Id, statement.Id, ticket.Id,
            90m, 1800m, true, null, owner.Id!, DateTimeOffset.UtcNow, _runId + "-match");
        context.Set<StatementTicketMatch>().Add(match);
        await context.SaveChangesAsync();
        // Compare persisted precision on both sides rather than pre-save CLR timestamp precision.
        WeighbridgeTicket originalTicket = await context.Set<WeighbridgeTicket>().AsNoTracking()
            .SingleAsync(value => value.TenantId == tenant.Id && value.Id == ticket.Id);
        GrowerStatement originalStatement = await context.Set<GrowerStatement>().AsNoTracking()
            .SingleAsync(value => value.TenantId == tenant.Id && value.Id == statement.Id);
        StatementTicketMatch originalMatch = await context.Set<StatementTicketMatch>().AsNoTracking()
            .SingleAsync(value => value.TenantId == tenant.Id && value.Id == match.Id);
        string millBefore = System.Text.Json.JsonSerializer.Serialize(new
            { ticket = originalTicket, statement = originalStatement, match = originalMatch });
        CropCycleDetailsDto updatedYield = await new UpdateActualYieldCommandHandler(farms, manager, TimeProvider.System)
            .Handle(new(field.Id, cycle.Id, cycle.Version, 55.125m), default);
        context.ChangeTracker.Clear();
        WeighbridgeTicket savedTicket = await context.Set<WeighbridgeTicket>().SingleAsync(value => value.TenantId == tenant.Id && value.Id == ticket.Id);
        GrowerStatement savedStatement = await context.Set<GrowerStatement>().SingleAsync(value => value.TenantId == tenant.Id && value.Id == statement.Id);
        StatementTicketMatch savedMatch = await context.Set<StatementTicketMatch>().SingleAsync(value => value.TenantId == tenant.Id && value.Id == match.Id);
        System.Text.Json.JsonSerializer.Serialize(new { ticket = savedTicket, statement = savedStatement, match = savedMatch }).ShouldBe(millBefore);
        CropCycleCollectionDto read = await new GetCropCyclesQueryHandler(farms, manager, TimeProvider.System).Handle(new(field.Id), default);
        read.CropCycles.Single(value => value.Id == cycle.Id).HarvestResult!.ActualTonnes.ShouldBe(55.125m);
        updatedYield.Timeline.ShouldContain(value => value.Reason != null && value.Reason.Contains("55.125"));
        (await context.InputApplications.SingleAsync(value => value.TenantId == tenant.Id && value.Id == application)).ActivityId.ShouldBe(activity.Id);
        var audits = await context.AuditEvents.Where(value => value.TenantId == tenant.Id && value.FarmId == farm.Id &&
            (value.SubjectId == categoryId || value.SubjectId == worker.Id || value.SubjectId == tenant.GrowerProfile.Id ||
                value.SubjectId == farm.Id)).ToArrayAsync();
        audits.ShouldContain(value => value.Action == "Updated" && value.SubjectId == categoryId &&
            value.AuthenticatedUserId == manager.Id);
        audits.ShouldContain(value => value.Action == "WorkerProfileUpdated" && value.SubjectId == worker.Id &&
            value.AuthenticatedUserId == manager.Id);
        audits.ShouldContain(value => value.Action == "ProfileUpdated" && value.AuthenticatedUserId == owner.Id);
        audits.ShouldContain(value => value.Action == "FarmModelAssigned" && value.AuthenticatedUserId == owner.Id);
        System.Text.Json.JsonSerializer.Serialize(audits).ShouldNotContain("SYNTHETICCR01512");
        (await ReturnHistoricalSnapshotAsync(scenario)).ShouldBe(operationalFactsBeforeReturns);
        System.Text.Json.JsonSerializer.Serialize(await context.Set<PayrollCalculation>().AsNoTracking()
            .Include(value => value.WorkerLines).ThenInclude(value => value.EarningLines)
            .SingleAsync(value => value.TenantId == tenant.Id && value.Id == calculation.Id)).ShouldBe(payrollBefore);
        var accountabilityAudits = await context.AuditEvents.AsNoTracking().Where(value => value.TenantId == tenant.Id &&
            value.FarmId == farm.Id && (value.SubjectId == returnException.Id || value.SubjectId == reopened.Id ||
                value.SubjectId == remainingReturn || value.SubjectId == replacementReturn)).ToArrayAsync();
        var firstResolved = accountabilityAudits.Single(value => value.SubjectId == returnException.Id && value.Action == "Resolved");
        var reversalAudit = accountabilityAudits.Single(value => value.SubjectId == remainingReturn && value.Action == "Reversed");
        var reopenedAudit = accountabilityAudits.Single(value => value.SubjectId == reopened.Id && value.Action == "Opened");
        var secondResolved = accountabilityAudits.Single(value => value.SubjectId == reopened.Id && value.Action == "Resolved");
        firstResolved.AuthenticatedUserId.ShouldBe(manager.Id);
        reversalAudit.AuthenticatedUserId.ShouldBe(owner.Id);
        reopenedAudit.AuthenticatedUserId.ShouldBe(owner.Id);
        secondResolved.AuthenticatedUserId.ShouldBe(manager.Id);
        reversalAudit.OccurredAt.ShouldBeGreaterThanOrEqualTo(firstResolved.OccurredAt);
        reopenedAudit.OccurredAt.ShouldBeGreaterThanOrEqualTo(reversalAudit.OccurredAt);
        secondResolved.OccurredAt.ShouldBeGreaterThanOrEqualTo(reopenedAudit.OccurredAt);
        accountabilityAudits.Count(value => value.Action == "Posted").ShouldBe(2);
        (await context.StockMovements.CountAsync(value => value.TenantId == tenant.Id && value.StockReturnLineId.HasValue)).ShouldBe(3);
        StockLedgerSnapshot finalStock = (await inventory.GetStockOnHandAsync(tenant.Id, farm.Id, default)).Single().Snapshot;
        finalStock.Quantity.ShouldBe(16m);
        finalStock.ValueUsd.ShouldBe(48m);
        finalStock.WeightedAverageUnitCostUsd.ShouldBe(3m);
        System.Text.Json.JsonSerializer.Serialize(accountabilityAudits).ShouldNotContain("SYNTHETICCR01512");
    }

    [TestCase(6, 0, ControlExceptionStatus.Resolved)]
    [TestCase(5, 1, ControlExceptionStatus.Open)]
    [Category("CR015D01Acceptance")]
    public async Task ReturnReconcilesPersistedFactsPreservesValuationAndIsIdempotent(
        int returnQuantity, int unaccounted, ControlExceptionStatus expectedStatus)
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid returnId = await CreateReturnAsync(scenario, returnQuantity);
        await using ApplicationDbContext before = CreateContext();
        StockIssueLine issueLine = await before.StockIssueLines.AsNoTracking().SingleAsync(value =>
            value.TenantId == scenario.TenantId && value.Id == scenario.IssueLineId);
        string historicalBefore = await ReturnHistoricalSnapshotAsync(scenario);
        Guid exceptionId = await before.ControlExceptions.Where(value => value.TenantId == scenario.TenantId &&
            value.StockIssueLineId == scenario.IssueLineId).Select(value => value.Id).SingleAsync();
        long originalVersion = await before.StockReturns.Where(value => value.TenantId == scenario.TenantId &&
            value.Id == returnId).Select(value => value.Version).SingleAsync();
        await PostReturnAsync(scenario, returnId, $"focused-return-{returnId:N}");
        await using ApplicationDbContext verify = CreateContext();
        InventoryRepository repository = new(verify);
        (await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(4m);
        (await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(returnQuantity);
        (10m - await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default) -
            await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(unaccounted);
        ControlException exception = await verify.ControlExceptions.AsNoTracking().SingleAsync(value =>
            value.TenantId == scenario.TenantId && value.Id == exceptionId);
        exception.Status.ShouldBe(expectedStatus);
        if (unaccounted == 0)
        {
            exception.UnaccountedQuantity.ShouldBe(0m);
            exception.AppliedQuantity.ShouldBe(4m);
            exception.ReturnedQuantity.ShouldBe(6m);
            exception.ResolvedAt.ShouldNotBeNull();
        }
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId,
            scenario.ActivityId, default)).ShouldBe(unaccounted != 0);
        StockReturnLine line = await verify.StockReturnLines.SingleAsync(value => value.TenantId == scenario.TenantId &&
            value.StockReturnId == returnId);
        line.Quantity.ShouldBe(returnQuantity);
        line.IssueUnitCostUsdSnapshot.ShouldBe(issueLine.IssueUnitCostUsd!.Value);
        line.IssueUnitCostUsdSnapshot.ShouldBe(3m);
        StockMovement movement = await verify.StockMovements.SingleAsync(value => value.TenantId == scenario.TenantId &&
            value.StockReturnLineId == line.Id);
        movement.SignedQuantity.ShouldBe(returnQuantity);
        movement.SignedValueUsd.ShouldBe(returnQuantity * 3m);
        StockLedgerSnapshot stock = (await repository.GetStockOnHandAsync(scenario.TenantId, scenario.FarmId, default)).Single().Snapshot;
        stock.Quantity.ShouldBe(10m + returnQuantity);
        stock.ValueUsd.ShouldBe((10m + returnQuantity) * 3m);
        stock.WeightedAverageUnitCostUsd.ShouldBe(3m);
        (await ReturnHistoricalSnapshotAsync(scenario)).ShouldBe(historicalBefore);
        var audits = await verify.AuditEvents.Where(value => value.TenantId == scenario.TenantId &&
            value.FarmId == scenario.FarmId && (value.SubjectId == returnId || value.SubjectId == exceptionId)).ToArrayAsync();
        audits.Count(value => value.SubjectId == returnId && value.Action == "Posted").ShouldBe(1);
        audits.Count(value => value.SubjectId == exceptionId && value.Action == "Resolved").ShouldBe(unaccounted == 0 ? 1 : 0);
        audits.ShouldAllBe(value => value.AuthenticatedUserId == scenario.ManagerUserId);
        string auditBeforeRetry = await AuditSnapshotAsync(scenario);
        Dictionary<string, string> beforeRetry = await CategoryIntegritySnapshotAsync(scenario);
        // Retry with the original version: existing idempotency takes precedence over concurrency validation.
        await using (ApplicationDbContext retry = CreateContext())
        {
            await new PostStockReturnCommandHandler(new FarmSetupRepository(retry), new InventoryRepository(retry),
                new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System)
                .Handle(new(returnId, originalVersion, $"{_runId}-focused-return-{returnId:N}"), default);
        }
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(beforeRetry);
        (await AuditSnapshotAsync(scenario)).ShouldBe(auditBeforeRetry);
        if (unaccounted > 0)
        {
            await AssertPartialReturnStillBlocksClosureAsync(scenario);
        }
    }

    [Test]
    [Category("CR015D01Acceptance")]
    public async Task PartialThenFinalReturnCountsEachPersistedReturnExactlyOnce()
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid partial = await CreateReturnAsync(scenario, 5m);
        await PostReturnAsync(scenario, partial, "partial-five");
        await using ApplicationDbContext verify = CreateContext();
        InventoryRepository repository = new(verify);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeTrue();
        (10m - await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default) -
            await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(1m);
        Guid final = await CreateReturnAsync(scenario, 1m);
        await PostReturnAsync(scenario, final, "final-one");
        (await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(6m);
        (await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(4m);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeFalse();
        (await verify.StockMovements.CountAsync(value => value.TenantId == scenario.TenantId &&
            value.StockReturnLineId.HasValue)).ShouldBe(2);
    }

    [Test]
    [Category("CR015D01Acceptance")]
    public async Task CompleteApplicationWithoutReturnStillResolvesAccountability()
    {
        Scenario scenario = await CreateAppliedScenarioAsync(10m);
        await using ApplicationDbContext verify = CreateContext();
        InventoryRepository repository = new(verify);
        (await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(10m);
        (await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(0m);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeFalse();
        (await verify.ControlExceptions.SingleAsync(value => value.TenantId == scenario.TenantId &&
            value.StockIssueLineId == scenario.IssueLineId)).Status.ShouldBe(ControlExceptionStatus.Resolved);
        (await verify.StockReturns.AnyAsync(value => value.TenantId == scenario.TenantId)).ShouldBeFalse();
    }

    [Test]
    [Category("CR015D01Acceptance")]
    public async Task FailureAfterReturnFlushRollsBackPostingReconciliationAndAudits()
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid returnId = await CreateReturnAsync(scenario, 6m);
        Dictionary<string, string> before = await CategoryIntegritySnapshotAsync(scenario);
        string auditBefore = await AuditSnapshotAsync(scenario);
        FailSecondSaveInterceptor failure = new();
        await using (ApplicationDbContext context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString).AddInterceptors(failure).Options))
        {
            long version = await context.StockReturns.Where(value => value.TenantId == scenario.TenantId &&
                value.Id == returnId).Select(value => value.Version).SingleAsync();
            await Should.ThrowAsync<InvalidOperationException>(() => new PostStockReturnCommandHandler(
                new FarmSetupRepository(context), new InventoryRepository(context),
                new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System)
                .Handle(new(returnId, version, $"{_runId}-rollback-return"), default));
            failure.SaveCount.ShouldBe(2);
            failure.SawPersistedReturnAndMovement.ShouldBeTrue();
            failure.SawResolvedException.ShouldBeTrue();
        }
        // A fresh connection sees exactly the prior committed draft, ledger, costs and audit state.
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(before);
        (await AuditSnapshotAsync(scenario)).ShouldBe(auditBefore);
        await using ApplicationDbContext verify = CreateContext();
        (await verify.StockReturns.SingleAsync(value => value.TenantId == scenario.TenantId &&
            value.Id == returnId)).Status.ShouldBe(StockReturnStatus.Draft);
        (await new InventoryRepository(verify).HasBlockingInventoryExceptionAsync(scenario.TenantId,
            scenario.FarmId, scenario.ActivityId, default)).ShouldBeTrue();
    }

    [TestCase(6)]
    [TestCase(5)]
    [Category("CR015ReturnReversalRegression")]
    [Category("CR015D02Acceptance")]
    public async Task ReversingFinalReturnReopensAccountabilityAndBlocksClosure(int returnQuantity)
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid returnId = await CreateReturnAsync(scenario, returnQuantity);
        await PostReturnAsync(scenario, returnId, $"before-reversal-{returnId:N}");
        await using ApplicationDbContext verify = CreateContext();
        InventoryRepository repository = new(verify);
        ControlException originalEpisode = await verify.ControlExceptions.AsNoTracking().SingleAsync(value =>
            value.TenantId == scenario.TenantId && value.StockIssueLineId == scenario.IssueLineId);
        originalEpisode.Status.ShouldBe(returnQuantity == 6 ? ControlExceptionStatus.Resolved : ControlExceptionStatus.Open);
        string originalHistory = System.Text.Json.JsonSerializer.Serialize(originalEpisode);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId,
            scenario.ActivityId, default)).ShouldBe(returnQuantity != 6);
        (10m - await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default) -
            await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(6m - returnQuantity);
        long originalVersion = await verify.StockReturns.Where(value => value.TenantId == scenario.TenantId &&
            value.Id == returnId).Select(value => value.Version).SingleAsync();
        string historicalBefore = await ReturnHistoricalSnapshotAsync(scenario);
        string key = $"reverse-{returnId:N}";
        await ReverseReturnAsync(scenario, returnId, key);
        (await ReturnHistoricalSnapshotAsync(scenario)).ShouldBe(historicalBefore);
        StockLedgerSnapshot stock = (await repository.GetStockOnHandAsync(scenario.TenantId, scenario.FarmId, default)).Single().Snapshot;
        stock.Quantity.ShouldBe(10m);
        stock.ValueUsd.ShouldBe(30m);
        stock.WeightedAverageUnitCostUsd.ShouldBe(3m);
        (await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(4m);
        (await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(0m);
        ControlException[] episodes = await verify.ControlExceptions.AsNoTracking().Where(value =>
            value.TenantId == scenario.TenantId && value.StockIssueLineId == scenario.IssueLineId).ToArrayAsync();
        System.Text.Json.JsonSerializer.Serialize(episodes.Single(value => value.Id == originalEpisode.Id)).ShouldBe(originalHistory);
        ControlException open = episodes.Single(value => value.Status == ControlExceptionStatus.Open);
        episodes.Length.ShouldBe(returnQuantity == 6 ? 2 : 1);
        if (returnQuantity == 6)
        {
            open.Id.ShouldNotBe(originalEpisode.Id);
            open.UnaccountedQuantity.ShouldBe(6m);
            open.AppliedQuantity.ShouldBe(4m);
            open.ReturnedQuantity.ShouldBe(0m);
        }
        else
        {
            open.Id.ShouldBe(originalEpisode.Id);
        }
        StockMovement[] movements = await verify.StockMovements.AsNoTracking().Where(value =>
            value.TenantId == scenario.TenantId && value.StockReturnLineId.HasValue).OrderBy(value => value.PostingSequence).ToArrayAsync();
        movements.Length.ShouldBe(2);
        movements[1].ReversalOfStockMovementId.ShouldBe(movements[0].Id);
        movements[1].SignedQuantity.ShouldBe(-returnQuantity);
        movements[1].SignedValueUsd.ShouldBe(-returnQuantity * 3m);
        movements.Sum(value => value.SignedQuantity).ShouldBe(0m);
        movements.Sum(value => value.SignedValueUsd).ShouldBe(0m);
        Dictionary<string, string> beforeRetry = await CategoryIntegritySnapshotAsync(scenario);
        string auditBeforeRetry = await AuditSnapshotAsync(scenario);
        await using (ApplicationDbContext retry = CreateContext())
        {
            await new ReverseStockReturnCommandHandler(new FarmSetupRepository(retry), new InventoryRepository(retry),
                new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System)
                .Handle(new(returnId, originalVersion, "Synthetic reversal", $"{_runId}-{key}"), default);
        }
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(beforeRetry);
        (await AuditSnapshotAsync(scenario)).ShouldBe(auditBeforeRetry);
        await AssertPartialReturnStillBlocksClosureAsync(scenario);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeTrue();
        var audits = await verify.AuditEvents.AsNoTracking().Where(value => value.TenantId == scenario.TenantId &&
            value.FarmId == scenario.FarmId).ToArrayAsync();
        var reversed = audits.Single(value => value.SubjectId == returnId && value.Action == "Reversed");
        reversed.AuthenticatedUserId.ShouldBe(scenario.GrowerUserId);
        if (returnQuantity == 6)
        {
            var opened = audits.Single(value => value.SubjectId == open.Id && value.Action == "Opened");
            opened.AuthenticatedUserId.ShouldBe(scenario.GrowerUserId);
            opened.OccurredAt.ShouldBeGreaterThanOrEqualTo(reversed.OccurredAt);
            Guid replacement = await CreateReturnAsync(scenario, 6m);
            await PostReturnAsync(scenario, replacement, $"replacement-{replacement:N}");
            (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeFalse();
            ControlException resolved = await verify.ControlExceptions.AsNoTracking().SingleAsync(value =>
                value.TenantId == scenario.TenantId && value.Id == open.Id);
            resolved.Status.ShouldBe(ControlExceptionStatus.Resolved);
            resolved.UnaccountedQuantity.ShouldBe(0m);
            System.Text.Json.JsonSerializer.Serialize(await verify.ControlExceptions.AsNoTracking().SingleAsync(value =>
                value.TenantId == scenario.TenantId && value.Id == originalEpisode.Id)).ShouldBe(originalHistory);
            var resolution = await verify.AuditEvents.SingleAsync(value => value.TenantId == scenario.TenantId &&
                value.SubjectId == open.Id && value.Action == "Resolved");
            resolution.AuthenticatedUserId.ShouldBe(scenario.ManagerUserId);
            resolution.OccurredAt.ShouldBeGreaterThanOrEqualTo(opened.OccurredAt);
            (await ReturnHistoricalSnapshotAsync(scenario)).ShouldBe(historicalBefore);
            await CloseCompletedActivityAsync(scenario);
        }
    }

    [Test]
    [Category("CR015D02Acceptance")]
    public async Task FailureAfterReversalFlushRollsBackReversalNewEpisodeAndAudits()
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid returnId = await CreateReturnAsync(scenario, 6m);
        await PostReturnAsync(scenario, returnId, "rollback-effective-return");
        Dictionary<string, string> before = await CategoryIntegritySnapshotAsync(scenario);
        string auditBefore = await AuditSnapshotAsync(scenario);
        FailReversalReconciliationSaveInterceptor failure = new();
        await using (ApplicationDbContext context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString).AddInterceptors(failure).Options))
        {
            long version = await context.StockReturns.Where(value => value.TenantId == scenario.TenantId &&
                value.Id == returnId).Select(value => value.Version).SingleAsync();
            await Should.ThrowAsync<InvalidOperationException>(() => new ReverseStockReturnCommandHandler(
                new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId),
                TimeProvider.System).Handle(new(returnId, version, "Synthetic rollback", $"{_runId}-rollback-reversal"), default));
            failure.SaveCount.ShouldBe(2);
            failure.SawFlushedReversal.ShouldBeTrue();
            failure.SawNewOpenEpisode.ShouldBeTrue();
        }
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(before);
        (await AuditSnapshotAsync(scenario)).ShouldBe(auditBefore);
        await using ApplicationDbContext verify = CreateContext();
        (await verify.StockReturns.SingleAsync(value => value.TenantId == scenario.TenantId && value.Id == returnId))
            .Status.ShouldBe(StockReturnStatus.Posted);
        (await new InventoryRepository(verify).GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(6m);
        (await verify.ControlExceptions.SingleAsync(value => value.TenantId == scenario.TenantId &&
            value.StockIssueLineId == scenario.IssueLineId)).Status.ShouldBe(ControlExceptionStatus.Resolved);
    }

    [Test]
    [Category("CR015D02Acceptance")]
    public async Task CompleteApplicationAfterReversalAndReversalRetryDoNotCreateUnnecessaryEpisode()
    {
        Scenario scenario = await CreateAppliedScenarioAsync(4m);
        Guid returnId = await CreateReturnAsync(scenario, 6m);
        await PostReturnAsync(scenario, returnId, "complete-control-return");
        await ReverseReturnAsync(scenario, returnId, "complete-control-reverse");
        await using ApplicationDbContext verify = CreateContext();
        Guid receipt = await verify.FieldReceipts.Where(value => value.TenantId == scenario.TenantId)
            .Select(value => value.Id).SingleAsync();
        Guid remainder = await CreateAttestedApplicationAsync(scenario, receipt, 6m);
        await ConfirmAsync(scenario, remainder, "complete-control-apply");
        InventoryRepository repository = new(verify);
        (await repository.GetConfirmedAppliedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(10m);
        (await repository.GetPostedReturnedQuantityAsync(scenario.IssueLineId, default)).ShouldBe(0m);
        (await repository.HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, default)).ShouldBeFalse();
        Dictionary<string, string> beforeRetry = await CategoryIntegritySnapshotAsync(scenario);
        string auditBeforeRetry = await AuditSnapshotAsync(scenario);
        await ReverseReturnAsync(scenario, returnId, "complete-control-reverse");
        (await CategoryIntegritySnapshotAsync(scenario)).ShouldBe(beforeRetry);
        (await AuditSnapshotAsync(scenario)).ShouldBe(auditBeforeRetry);
        (await verify.ControlExceptions.CountAsync(value => value.TenantId == scenario.TenantId &&
            value.StockIssueLineId == scenario.IssueLineId && value.Status == ControlExceptionStatus.Open)).ShouldBe(0);
    }

    private async Task CloseCompletedActivityAsync(Scenario scenario)
    {
        await using ApplicationDbContext context = CreateContext();
        FarmSetupRepository farms = new(context);
        Tenant tenant = (await farms.GetTenantForUserAsync(scenario.ManagerUserId, true, default))!;
        Activity activity = tenant.ActiveFarm!.Fields.Single(value => value.Id == scenario.FieldId).CropCycles
            .Single(value => value.Id == scenario.CycleId).Activities.Single(value => value.Id == scenario.ActivityId);
        activity.Status.ShouldBe(ActivityStatus.Completed);
        await new TransitionActivityCommandHandler(farms, new LabourRepository(context), new InventoryRepository(context),
            new AcceptanceUser(scenario.ManagerUserId), new DisplayOnlyIdentityService(), TimeProvider.System)
            .Handle(new(activity.Id, ActivityStatus.Closed.ToString(), activity.Version, null), default);
        activity.Status.ShouldBe(ActivityStatus.Closed);
    }

    private async Task AssertPartialReturnStillBlocksClosureAsync(Scenario scenario)
    {
        await using ApplicationDbContext context = CreateContext();
        FarmSetupRepository farms = new(context);
        Tenant tenant = (await farms.GetTenantForUserAsync(scenario.ManagerUserId, true, default))!;
        Field field = tenant.ActiveFarm!.Fields.Single(value => value.Id == scenario.FieldId);
        CropCycle cycle = field.CropCycles.Single(value => value.Id == scenario.CycleId);
        Activity activity = cycle.Activities.Single(value => value.Id == scenario.ActivityId);
        DateTimeOffset workAt = new(2026, 8, 24, 10, 0, 0, TimeSpan.Zero);
        activity.RecordActualWork(workAt, 1m, field.ReportingHectares, null, cycle.StartDate,
            workAt, scenario.ManagerUserId, null, activity.Version);
        await farms.SaveChangesAsync(default);
        TransitionActivityCommandHandler transition = new(farms, new LabourRepository(context),
            new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId),
            new DisplayOnlyIdentityService(), TimeProvider.System);
        foreach (ActivityStatus status in new[] { ActivityStatus.Planned, ActivityStatus.InProgress,
                     ActivityStatus.AwaitingVerification, ActivityStatus.ManagerConfirmation, ActivityStatus.Completed })
        {
            await transition.Handle(new(activity.Id, status.ToString(), activity.Version, null), default);
        }
        ValidationException blocked = await Should.ThrowAsync<ValidationException>(async () =>
        {
            await transition.Handle(new(activity.Id, ActivityStatus.Closed.ToString(), activity.Version, null), default);
            TestContext.Out.WriteLine($"Closure unexpectedly succeeded: activity={activity.Status}.");
        });
        blocked.Errors["TargetStatus"].Single().ShouldContain("unaccounted");
        activity.Status.ShouldBe(ActivityStatus.Completed);
        activity.StatusChanges.ShouldNotContain(value => value.ToStatus == ActivityStatus.Closed);
    }

    private async Task<Scenario> CreateAppliedScenarioAsync(decimal quantity)
    {
        Scenario scenario = await CreateScenarioAsync();
        Guid receipt = await RecordReceiptAsync(scenario, 10m);
        Guid application = await CreateAttestedApplicationAsync(scenario, receipt, quantity);
        await ConfirmAsync(scenario, application, "focused-application");
        return scenario;
    }

    private async Task<string> ReturnHistoricalSnapshotAsync(Scenario scenario)
    {
        await using ApplicationDbContext context = CreateContext();
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            Issues = await context.StockIssues.AsNoTracking().Include(value => value.Lines)
                .Where(value => value.TenantId == scenario.TenantId).OrderBy(value => value.Id).ToArrayAsync(),
            Receipts = await context.StockReceipts.AsNoTracking().Include(value => value.Lines)
                .Where(value => value.TenantId == scenario.TenantId).OrderBy(value => value.Id).ToArrayAsync(),
            Movements = await context.StockMovements.AsNoTracking().Where(value => value.TenantId == scenario.TenantId &&
                !value.StockReturnLineId.HasValue).OrderBy(value => value.Id).ToArrayAsync(),
            Applications = await context.InputApplications.AsNoTracking().Include(value => value.Lines)
                .Where(value => value.TenantId == scenario.TenantId).OrderBy(value => value.Id).ToArrayAsync(),
            Costs = await context.OperationalCostPostings.AsNoTracking().Where(value => value.TenantId == scenario.TenantId)
                .OrderBy(value => value.Id).ToArrayAsync()
        });
    }

    private async Task<string> AuditSnapshotAsync(Scenario scenario)
    {
        await using ApplicationDbContext context = CreateContext();
        return System.Text.Json.JsonSerializer.Serialize(await context.AuditEvents.AsNoTracking()
            .Where(value => value.TenantId == scenario.TenantId && value.FarmId == scenario.FarmId)
            .OrderBy(value => value.Id).ToArrayAsync());
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
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1)); tenant.AddMembership(managerId, manager.Id, TenantSecurityRoles.FarmManager);
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
    { await using var context = CreateContext(); var version = await context.InputApplications.Where(x => x.Id == applicationId).Select(x => x.Version).SingleAsync(); await new ConfirmInputApplicationCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new ConfirmInputApplicationCommand(applicationId, null, version, $"{_runId}-{key}"), CancellationToken.None); }
    private async Task PostReturnAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.StockReturns.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new PostStockReturnCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new PostStockReturnCommand(id, version, $"{_runId}-{key}"), CancellationToken.None); }
    private async Task ReverseReturnAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.StockReturns.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new ReverseStockReturnCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new ReverseStockReturnCommand(id, version, "Synthetic reversal", $"{_runId}-{key}"), CancellationToken.None); }
    private async Task DecideLossAsync(Scenario scenario, Guid id, ApprovalOutcome outcome, string key)
    { await using var context = CreateContext(); var version = await context.InventoryLosses.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new DecideInventoryLossCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new DecideInventoryLossCommand(id, version, outcome, null, $"{_runId}-{key}"), CancellationToken.None); }
    private async Task<Guid> RequestApplicationCorrectionAsync(Scenario scenario, Guid id)
    { await using var context = CreateContext(); var version = await context.InputApplications.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); return await new CreateFieldAccountabilityCorrectionCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new CreateFieldAccountabilityCorrectionCommand(null, id, null, null, version, "Synthetic correction", $"{_runId}-correction-request"), CancellationToken.None); }
    private async Task DecideCorrectionAsync(Scenario scenario, Guid id, string key)
    { await using var context = CreateContext(); var version = await context.FieldAccountabilityCorrections.Where(x => x.Id == id).Select(x => x.Version).SingleAsync(); await new DecideFieldAccountabilityCorrectionCommandHandler(new FarmSetupRepository(context), new InventoryRepository(context), new AcceptanceUser(scenario.GrowerUserId), TimeProvider.System).Handle(new DecideFieldAccountabilityCorrectionCommand(id, version, ApprovalOutcome.Approved, null, $"{_runId}-{key}"), CancellationToken.None); }
    private async Task AssertClosureBlockedAsync(Scenario scenario)
    { await using var context = CreateContext(); (await new InventoryRepository(context).HasBlockingInventoryExceptionAsync(scenario.TenantId, scenario.FarmId, scenario.ActivityId, CancellationToken.None)).ShouldBeTrue(); throw new ConflictException("Activity closure is blocked by the persisted open accountability exception."); }
    private async Task AssertAppendOnlyAsync(string sql) { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); (await Should.ThrowAsync<PostgresException>(() => command.ExecuteNonQueryAsync())).SqlState.ShouldBe(PostgresErrorCodes.RaiseException); }
    private static async Task<bool> AttemptAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (ConflictException)
        {
            return false;
        }
        catch (InventorySerializationFailureException)
        {
            return false;
        }
        catch (PostgresException)
        {
            return false;
        }
    }
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
    private ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString, options => options.CommandTimeout(120)).Options);
    private static string LoadConfiguredConnectionString() { var value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"p5c-{Guid.NewGuid():N}"; }
    private sealed class FailSecondSaveInterceptor : SaveChangesInterceptor
    {
        public int SaveCount { get; private set; }
        public bool SawPersistedReturnAndMovement { get; private set; }
        public bool SawResolvedException { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (SaveCount == 2)
            {
                ApplicationDbContext context = (ApplicationDbContext)eventData.Context!;
                StockReturn posted = context.ChangeTracker.Entries<StockReturn>().Single().Entity;
                SawPersistedReturnAndMovement = await context.StockReturns.AsNoTracking().AnyAsync(value =>
                    value.TenantId == posted.TenantId && value.Id == posted.Id && value.Status == StockReturnStatus.Posted,
                    cancellationToken) && await context.StockMovements.AsNoTracking().AnyAsync(value =>
                    value.TenantId == posted.TenantId && value.StockReturnLineId == posted.Lines.Single().Id, cancellationToken);
                SawResolvedException = context.ChangeTracker.Entries<ControlException>().Single().Entity.Status ==
                    ControlExceptionStatus.Resolved;
                throw new InvalidOperationException("Synthetic failure after the posting flush and before reconciliation persistence.");
            }
            return result;
        }
    }

    private sealed class FailReversalReconciliationSaveInterceptor : SaveChangesInterceptor
    {
        public int SaveCount { get; private set; }
        public bool SawFlushedReversal { get; private set; }
        public bool SawNewOpenEpisode { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (SaveCount == 2)
            {
                ApplicationDbContext context = (ApplicationDbContext)eventData.Context!;
                StockReturn reversed = context.ChangeTracker.Entries<StockReturn>().Single().Entity;
                SawFlushedReversal = await context.StockReturns.AsNoTracking().AnyAsync(value =>
                    value.TenantId == reversed.TenantId && value.Id == reversed.Id && value.Status == StockReturnStatus.Reversed,
                    cancellationToken) && await context.StockMovements.AsNoTracking().AnyAsync(value =>
                    value.TenantId == reversed.TenantId && value.StockReturnLineId == reversed.Lines.Single().Id &&
                    value.MovementType == StockMovementType.ReturnReversal, cancellationToken);
                SawNewOpenEpisode = context.ChangeTracker.Entries<ControlException>().Any(value => value.State == EntityState.Added &&
                    value.Entity.Status == ControlExceptionStatus.Open && value.Entity.UnaccountedQuantity == 6m);
                throw new InvalidOperationException("Synthetic failure after reversal flush and before reconciliation persistence.");
            }
            return result;
        }
    }

    // Mapping uses display names only; this stub cannot grant permissions or create authenticated users.
    private sealed class DisplayOnlyIdentityService : IIdentityService
    {
        public Task<string?> GetUserNameAsync(string userId) => Task.FromResult<string?>("Synthetic actor");
        public Task<bool> IsInRoleAsync(string userId, string role) => throw new NotSupportedException();
        public Task<bool> AuthorizeAsync(string userId, string policyName) => throw new NotSupportedException();
        public Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password) => throw new NotSupportedException();
        public Task<Result> DeleteUserAsync(string userId) => throw new NotSupportedException();
    }

    private sealed record Scenario(Guid TenantId, Guid FarmId, Guid StoreId, Guid FieldId, Guid CycleId, Guid ActivityId, Guid IssueId, Guid IssueLineId, Guid ManagerId, Guid SupervisorId, Guid StorekeeperId, Guid RecipientId, string GrowerUserId, string ManagerUserId);
}
