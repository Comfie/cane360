using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.FarmSetup;
using Cane360.Application.Finance;
using Cane360.Application.Labour;
using Cane360.Application.MillRecords;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using Cane360.Domain.Labour;
using Cane360.Domain.MillRecords;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Synthetic Railway Development performance fixture; never applies migrations or deletes data.")]
[Category("Phase8APerformance")]
[NonParallelizable]
public sealed class Phase8APerformanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task VerifyTarget()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        _connectionString = configuration.GetConnectionString("Cane360Db") ??
            throw new InvalidOperationException("Railway Development configuration is required.");
        new NpgsqlConnectionStringBuilder(_connectionString).Host.ShouldEndWith(".rlwy.net");
        await using ApplicationDbContext context = Context();
        (await context.Database.GetAppliedMigrationsAsync()).Count().ShouldBe(17);
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        context.Database.HasPendingModelChanges().ShouldBeFalse();
        TestContext.Progress.WriteLine("Target: Railway Development; 17 applied, 0 pending; EF parity clean.");
    }

    [Test]
    public async Task MeasureFarmContextAndMillRepositoryOnGrowingSyntheticHistory()
    {
        string? reuse = Environment.GetEnvironmentVariable("P8A_FIXTURE_LABEL");
        string label = reuse ?? $"AUTOTEST-P8A-PERF-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        label.ShouldStartWith("AUTOTEST-P8A-PERF-");
        if (reuse is null) await SeedAsync(label);
        await using ApplicationDbContext lookup = Context();
        var farm = await lookup.Farms.AsNoTracking().Where(x => x.Name == label)
            .Select(x => new { x.Id, x.TenantId }).SingleAsync();
        string userId = await lookup.TenantMemberships.Where(x => x.TenantId == farm.TenantId)
            .Select(x => x.UserId).SingleAsync();
        await EnsureInventoryFixtureAsync(farm.TenantId, farm.Id, userId, label);
        await EnsurePayrollFixtureAsync(farm.TenantId, farm.Id, userId);
        await EnsureFinanceFixtureAsync(farm.TenantId, farm.Id, userId, label);
        var profile = new
        {
            farms = await lookup.Farms.CountAsync(x => x.TenantId == farm.TenantId),
            fields = await lookup.Fields.CountAsync(x => x.FarmId == farm.Id),
            cycles = await lookup.CropCycles.CountAsync(x => lookup.Fields.Any(f => f.FarmId == farm.Id && f.Id == x.FieldId)),
            activities = await lookup.Activities.CountAsync(x => x.TenantId == farm.TenantId),
            workers = await lookup.WorkerProfiles.CountAsync(x => x.TenantId == farm.TenantId),
            attendanceWork = await lookup.Attendances.CountAsync(x => x.TenantId == farm.TenantId),
            inventoryMovements = await lookup.StockMovements.CountAsync(x => x.TenantId == farm.TenantId),
            payrollLines = await lookup.PayrollEarningLines.CountAsync(x => x.TenantId == farm.TenantId),
            financeTransactions = await lookup.OperationalTransactions.CountAsync(x => x.TenantId == farm.TenantId),
            costPostings = await lookup.OperationalCostPostings.CountAsync(x => x.TenantId == farm.TenantId),
            tickets = await lookup.WeighbridgeTickets.CountAsync(x => x.TenantId == farm.TenantId),
            statements = await lookup.GrowerStatements.CountAsync(x => x.TenantId == farm.TenantId),
            auditEvents = await lookup.AuditEvents.CountAsync(x => x.TenantId == farm.TenantId)
        };
        profile.activities.ShouldBe(2000);
        profile.attendanceWork.ShouldBe(5000);
        profile.inventoryMovements.ShouldBe(3000);
        profile.payrollLines.ShouldBe(1000);
        profile.financeTransactions.ShouldBe(2000);
        profile.costPostings.ShouldBe(500);
        profile.tickets.ShouldBe(2000);
        TestContext.Progress.WriteLine($"Fixture: {label}; profile: {JsonSerializer.Serialize(profile)}");
        await MeasureAsync("Farm reference context repository", async () =>
        {
            await using ApplicationDbContext context = Context();
            Tenant? tenant = await new FarmSetupRepository(context).GetTenantReferenceContextForUserAsync(userId, false, default);
            tenant.ShouldNotBeNull();
            tenant.Id.ShouldBe(farm.TenantId);
        });
        await MeasureAsync("Mill ticket page service, 50 of 2000 rows", async () =>
        {
            await using ApplicationDbContext context = Context();
            var service = new MillRecordsService(new FarmSetupRepository(context),
                new MillRecordsRepository(context), new UnusedEvidenceStorage(), new SyntheticUser(userId), TimeProvider.System);
            var page = await service.GetTicketPageAsync(new(null, null, null, null, null, null, null, null), 1, 50, default);
            page.Items.Count.ShouldBe(50);
            page.TotalCount.ShouldBe(2000);
            page.RecordedNetTonnes.ShouldBe(30000m);
            page.UnmatchedCount.ShouldBe(2000);
        });
        await MeasureAsync("Statement reconciliation page service, 50 of 100 rows", async () =>
        {
            await using ApplicationDbContext context = Context();
            var service = new MillRecordsService(new FarmSetupRepository(context),
                new MillRecordsRepository(context), new UnusedEvidenceStorage(), new SyntheticUser(userId), TimeProvider.System);
            GrowerStatementPageDto page = await service.GetStatementPageAsync(
                new(null, null, null, "Unmatched", "AUTOTEST-P8A-S-"), 1, 50, default);
            page.Items.Count.ShouldBe(50);
            page.TotalCount.ShouldBe(100);
            page.Items.ShouldAllBe(x => x.Reconciliation.Status == "Unmatched");
        });
        await MeasureAsync("Statement reconciliation report service, all 100 rows", async () =>
        {
            await using ApplicationDbContext context = Context();
            var service = new MillRecordsService(new FarmSetupRepository(context),
                new MillRecordsRepository(context), new UnusedEvidenceStorage(), new SyntheticUser(userId), TimeProvider.System);
            IReadOnlyList<GrowerStatementDto> rows = await service.GetStatementsAsync(
                new(null, null, null, null, "AUTOTEST-P8A-S-"), default);
            rows.Count.ShouldBe(100);
        }, 4000);
        await MeasureAsync("Finance transaction page service, 50 of 2000 rows", async () =>
        {
            await using ApplicationDbContext context = Context();
            var service = new FinanceService(new FarmSetupRepository(context),
                new FinanceRepository(context), new UnusedPayrollProjection(),
                new SyntheticUser(userId), TimeProvider.System);
            OperationalTransactionPageDto page = await service.GetTransactionPageAsync(
                new(null, null, null, null, null, "AUTOTEST-P8A-FIN-"), 1, 50, default);
            page.Items.Count.ShouldBe(50);
            page.TotalCount.ShouldBe(2000);
            page.PostedExpenseUsd.ShouldBe(500m * 10m);
            page.DraftCount.ShouldBe(1500);
        });
        await MeasureAsync("Payroll run register summary, 1 run", async () =>
        {
            await using ApplicationDbContext context = Context();
            var handler = new GetPayrollRunsQueryHandler(new FarmSetupRepository(context),
                new PayrollRepository(context), new SyntheticUser(userId));
            IReadOnlyList<PayrollRunDto> runs = await handler.Handle(new GetPayrollRunsQuery(), default);
            runs.Count.ShouldBe(1);
            runs[0].Calculation.ShouldBeNull();
        });
        await MeasureAsync("Selected payroll run detail, 1000 earning lines", async () =>
        {
            await using ApplicationDbContext context = Context();
            Guid runId = await context.PayrollRuns.Where(x => x.TenantId == farm.TenantId &&
                x.FarmId == farm.Id).Select(x => x.Id).SingleAsync();
            var handler = new GetPayrollRunQueryHandler(new FarmSetupRepository(context),
                new PayrollRepository(context), new SyntheticUser(userId));
            PayrollRunDto run = await handler.Handle(new GetPayrollRunQuery(runId), default);
            run.Calculation!.Workers.Sum(x => x.Earnings.Count).ShouldBe(1000);
        }, 4000);
        await using (ApplicationDbContext context = Context())
        {
            var repository = new MillRecordsRepository(context);
            TicketFilter filter = new(null, null, null, null, null, null, "Unmatched", "AUTOTEST-P8A-T-");
            var first = await repository.GetTicketPageAsync(farm.TenantId, farm.Id, filter, 1, 50, default);
            var repeated = await repository.GetTicketPageAsync(farm.TenantId, farm.Id, filter, 1, 50, default);
            var second = await repository.GetTicketPageAsync(farm.TenantId, farm.Id, filter, 2, 50, default);
            first.Tickets.Select(x => x.Id).ShouldBe(repeated.Tickets.Select(x => x.Id));
            first.Tickets.Select(x => x.Id).Intersect(second.Tickets.Select(x => x.Id)).ShouldBeEmpty();
            second.TotalCount.ShouldBe(2000);
            var foreign = await repository.GetTicketPageAsync(Guid.NewGuid(), farm.Id, filter, 1, 50, default);
            foreign.Tickets.ShouldBeEmpty();
            foreign.TotalCount.ShouldBe(0);
            StatementFilter statementFilter = new(null, null, null, "Unmatched", "AUTOTEST-P8A-S-");
            MillStatementPageSource statementFirst = await repository.GetStatementPageSourceAsync(
                farm.TenantId, farm.Id, statementFilter, 1, 50, default);
            MillStatementPageSource statementRepeated = await repository.GetStatementPageSourceAsync(
                farm.TenantId, farm.Id, statementFilter, 1, 50, default);
            MillStatementPageSource statementSecond = await repository.GetStatementPageSourceAsync(
                farm.TenantId, farm.Id, statementFilter, 2, 50, default);
            statementFirst.Statements.Select(x => x.Id).ShouldBe(statementRepeated.Statements.Select(x => x.Id));
            statementFirst.Statements.Select(x => x.Id).Intersect(statementSecond.Statements.Select(x => x.Id)).ShouldBeEmpty();
            statementSecond.TotalCount.ShouldBe(100);
            MillStatementPageSource foreignStatements = await repository.GetStatementPageSourceAsync(
                Guid.NewGuid(), farm.Id, statementFilter, 1, 50, default);
            foreignStatements.Statements.ShouldBeEmpty();
            foreignStatements.TotalCount.ShouldBe(0);

            var financeRepository = new FinanceRepository(context);
            FinanceTransactionPageSource financeFirst = await financeRepository.GetTransactionPageAsync(
                farm.TenantId, farm.Id, null, null, null, null, null, "AUTOTEST-P8A-FIN-", 1, 50, default);
            FinanceTransactionPageSource financeRepeated = await financeRepository.GetTransactionPageAsync(
                farm.TenantId, farm.Id, null, null, null, null, null, "AUTOTEST-P8A-FIN-", 1, 50, default);
            FinanceTransactionPageSource financeSecond = await financeRepository.GetTransactionPageAsync(
                farm.TenantId, farm.Id, null, null, null, null, null, "AUTOTEST-P8A-FIN-", 2, 50, default);
            financeFirst.Transactions.Select(x => x.Id).ShouldBe(financeRepeated.Transactions.Select(x => x.Id));
            financeFirst.Transactions.Select(x => x.Id).Intersect(financeSecond.Transactions.Select(x => x.Id)).ShouldBeEmpty();
            financeSecond.TotalCount.ShouldBe(2000);
            financeSecond.PostedExpenseUsd.ShouldBe(5000m);
            financeSecond.DraftCount.ShouldBe(1500);
            FinanceTransactionPageSource postedFinance = await financeRepository.GetTransactionPageAsync(
                farm.TenantId, farm.Id, null, null, "Expense", "OtherExpense", "Posted",
                "AUTOTEST-P8A-FIN-", 1, 50, default);
            postedFinance.TotalCount.ShouldBe(500);
            postedFinance.PostedExpenseUsd.ShouldBe(5000m);
            postedFinance.DraftCount.ShouldBe(0);
            FinanceTransactionPageSource foreignFinance = await financeRepository.GetTransactionPageAsync(
                Guid.NewGuid(), farm.Id, null, null, null, null, null,
                "AUTOTEST-P8A-FIN-", 1, 50, default);
            foreignFinance.Transactions.ShouldBeEmpty();
            foreignFinance.TotalCount.ShouldBe(0);
        }
        await MeasureAsync("Mill ticket repository, all 2000 rows", async () =>
        {
            await using ApplicationDbContext context = Context();
            var rows = await new MillRecordsRepository(context).GetTicketsAsync(farm.TenantId,
                farm.Id, new(null, null, null, null, null, null, null, null), default);
            rows.Count.ShouldBe(2000);
        });
        await VerifyCorrectionExportParityAsync(farm.TenantId, farm.Id, userId);
        TestContext.Progress.WriteLine("Diagnostic repository/service timings only: authenticated HTTP acceptance remains required.");
    }

    [Test]
    public async Task MeasurePayrollSummaryAndSelectedDetailOnExistingSyntheticFixture()
    {
        string label = Environment.GetEnvironmentVariable("P8A_FIXTURE_LABEL") ??
            throw new InvalidOperationException("P8A_FIXTURE_LABEL is required for the focused read-only benchmark.");
        label.ShouldStartWith("AUTOTEST-P8A-PERF-");
        await using ApplicationDbContext lookup = Context();
        var farm = await lookup.Farms.AsNoTracking().Where(x => x.Name == label)
            .Select(x => new { x.Id, x.TenantId }).SingleAsync();
        string userId = await lookup.TenantMemberships.Where(x => x.TenantId == farm.TenantId)
            .Select(x => x.UserId).SingleAsync();
        Guid runId = await lookup.PayrollRuns.Where(x => x.TenantId == farm.TenantId &&
            x.FarmId == farm.Id).Select(x => x.Id).SingleAsync();

        await MeasureAsync("Payroll run register summary, 1 run", async () =>
        {
            await using ApplicationDbContext context = Context();
            var handler = new GetPayrollRunsQueryHandler(new FarmSetupRepository(context),
                new PayrollRepository(context), new SyntheticUser(userId));
            IReadOnlyList<PayrollRunDto> runs = await handler.Handle(new GetPayrollRunsQuery(), default);
            runs.Count.ShouldBe(1);
            runs[0].Calculation.ShouldBeNull();
        });
        await MeasureAsync("Selected payroll run detail, 1000 earning lines", async () =>
        {
            await using ApplicationDbContext context = Context();
            var handler = new GetPayrollRunQueryHandler(new FarmSetupRepository(context),
                new PayrollRepository(context), new SyntheticUser(userId));
            PayrollRunDto run = await handler.Handle(new GetPayrollRunQuery(runId), default);
            run.Calculation!.Workers.Sum(x => x.Earnings.Count).ShouldBe(1000);
        }, 4000);
        TestContext.Progress.WriteLine("Focused read-only payroll diagnostics; the labelled fixture was not mutated or cleaned.");
    }

    [Test]
    public async Task MeasureDashboardAndWorkerRegisterOnExistingSyntheticFixture()
    {
        string label = Environment.GetEnvironmentVariable("P8A_FIXTURE_LABEL") ??
            throw new InvalidOperationException("P8A_FIXTURE_LABEL is required for the focused read-only benchmark.");
        label.ShouldStartWith("AUTOTEST-P8A-PERF-");
        await using ApplicationDbContext lookup = Context();
        var farm = await lookup.Farms.AsNoTracking().Where(x => x.Name == label)
            .Select(x => new { x.Id, x.TenantId }).SingleAsync();
        string userId = await lookup.TenantMemberships.Where(x => x.TenantId == farm.TenantId)
            .Select(x => x.UserId).SingleAsync();

        await MeasureAsync("Dashboard farm setup, 50 fields and 100 cycles", async () =>
        {
            await using ApplicationDbContext context = Context();
            FarmSetupDto setup = await new GetFarmSetupQueryHandler(new FarmSetupRepository(context),
                new SyntheticUser(userId)).Handle(new GetFarmSetupQuery(), default);
            setup.Farm!.Fields.Count.ShouldBe(50);
        });
        await MeasureAsync("Worker register, 250 workers", async () =>
        {
            await using ApplicationDbContext context = Context();
            IReadOnlyList<WorkerListItemDto> workers = await new GetWorkersQueryHandler(
                new FarmSetupRepository(context), new LabourRepository(context),
                new SyntheticUser(userId)).Handle(new GetWorkersQuery(), default);
            workers.Count.ShouldBe(250);
        });
        TestContext.Progress.WriteLine("Focused dashboard/worker diagnostics; the labelled fixture was not mutated or cleaned.");
    }

    private async Task VerifyCorrectionExportParityAsync(Guid tenantId, Guid farmId, string userId)
    {
        await using ApplicationDbContext context = Context();
        // Only this transaction's synthetic correction is rolled back. Existing fixture
        // records remain untouched, and no cleanup runs against the shared database.
        await using var transaction = await context.Database.BeginTransactionAsync();
        WeighbridgeTicket original = await context.WeighbridgeTickets.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId)
            .OrderBy(x => x.TicketDate).ThenBy(x => x.Id).FirstAsync();
        var repository = new MillRecordsRepository(context);
        TicketFilter filter = new(original.TicketDate, original.TicketDate, null, null, null,
            "Recorded", "Unmatched", null);
        var before = await repository.GetTicketReportSourceAsync(tenantId, farmId, filter, default);
        string key = $"AUTOTEST-P8A-C-{Guid.NewGuid():N}";
        WeighbridgeTicket correction = WeighbridgeTicket.CreateCorrection(original, key,
            original.TicketDate.AddYears(1), 22m, 5m, 17m, null, null, null, null,
            "Synthetic out-of-range correction regression", userId, DateTimeOffset.UtcNow);
        correction.Record(userId, DateTimeOffset.UtcNow, key, correction.Version);
        context.WeighbridgeTickets.Add(correction);
        await context.SaveChangesAsync();

        var page = await repository.GetTicketPageAsync(tenantId, farmId, filter, 1, 100, default);
        var export = await repository.GetTicketReportSourceAsync(tenantId, farmId, filter, default);
        page.Tickets.ShouldNotContain(x => x.Id == original.Id || x.Id == correction.Id);
        page.TotalCount.ShouldBe(before.TotalCount - 1);
        page.RecordedNetTonnes.ShouldBe(before.RecordedNetTonnes - original.NetTonnes);
        export.Tickets.Select(x => x.Id).ShouldBe(page.Tickets.Select(x => x.Id));
        export.RecordedNetTonnes.ShouldBe(page.RecordedNetTonnes);
        export.Tickets.Sum(x => x.NetTonnes).ShouldBe(page.RecordedNetTonnes);
        await transaction.RollbackAsync();
        TestContext.Progress.WriteLine("Correction outside date filter: superseded original excluded; screen/export rows and totals reconcile; own uncommitted correction rolled back.");
    }

    private static async Task MeasureAsync(string workload, Func<Task> action, int thresholdMs = 2000)
    {
        await action();
        List<double> samples = [];
        for (int run = 0; run < 20; run++)
        {
            long start = Stopwatch.GetTimestamp();
            await action();
            samples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        samples.Sort();
        TestContext.Progress.WriteLine(JsonSerializer.Serialize(new
        {
            workload, runs = samples.Count, p50Ms = samples[9], p95Ms = samples[18],
            maxMs = samples[19], thresholdMs, meetsDiagnosticThreshold = samples[18] < thresholdMs
        }));
    }

    private async Task EnsureFinanceFixtureAsync(Guid tenantId, Guid farmId, string userId, string label)
    {
        await using ApplicationDbContext context = Context();
        var scope = await context.Fields.AsNoTracking().Where(x => x.FarmId == farmId)
            .Select(x => new { FieldId = x.Id, CycleId = x.CropCycles.OrderBy(c => c.StartDate).Select(c => c.Id).First() })
            .FirstAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string sourcePrefix = "AUTOTEST-P8A-FIN-";
        int existingTransactions = await context.OperationalTransactions.CountAsync(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.SourceReference != null &&
            x.SourceReference.StartsWith(sourcePrefix));
        if (existingTransactions == 0)
        {
            for (int index = 0; index < 2000; index++)
            {
                OperationalTransaction transaction = OperationalTransaction.Create(tenantId, farmId,
                    OperationalTransactionType.Expense, OperationalFinanceCategory.OtherExpense,
                    new DateOnly(2041, 1, 1).AddDays(index % 365), $"AUTOTEST-P8A payee {index:D5}",
                    10m, $"{sourcePrefix}{index:D5}", label, userId, now.AddTicks(index),
                    $"AUTOTEST-P8A-finance-{index:D5}");
                if (index < 500)
                {
                    TransactionAllocation allocation = TransactionAllocation.Create(tenantId, farmId,
                        transaction.Id, scope.CycleId, scope.FieldId, OperationalFinanceCategory.OtherExpense,
                        10m, TransactionAllocationType.CropCycleDirect, now.AddTicks(index));
                    transaction.ReplaceAllocations([allocation], transaction.Version);
                }
                context.OperationalTransactions.Add(transaction);
            }
            await context.SaveChangesAsync();
            existingTransactions = 2000;
        }
        existingTransactions.ShouldBe(2000, "The labelled performance fixture must not be partially populated.");

        List<OperationalTransaction> allocatedTransactions = await context.OperationalTransactions
            .Include(x => x.Allocations)
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.SourceReference != null &&
                x.SourceReference.StartsWith(sourcePrefix) && x.Allocations.Any())
            .ToListAsync();
        foreach (OperationalTransaction transaction in allocatedTransactions.Where(x =>
            x.Status == OperationalTransactionStatus.Draft))
        {
            transaction.Post(userId, now, $"AUTOTEST-P8A-post-{transaction.Id:N}", transaction.Version);
        }
        await context.SaveChangesAsync();

        HashSet<Guid> projectedAllocationIds = (await context.OperationalCostPostings.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId && x.TransactionAllocationId != null)
            .Select(x => x.TransactionAllocationId!.Value).ToListAsync()).ToHashSet();
        foreach (OperationalTransaction transaction in allocatedTransactions)
        {
            TransactionAllocation allocation = transaction.Allocations.Single();
            if (!projectedAllocationIds.Contains(allocation.Id))
                context.OperationalCostPostings.Add(OperationalCostPosting.ForDirectExpense(tenantId,
                    farmId, scope.FieldId, scope.CycleId, allocation, $"AUTOTEST-P8A-cost-{allocation.Id:N}"));
        }
        await context.SaveChangesAsync();
        TestContext.Progress.WriteLine("Extended only the labelled fixture with 2,000 finance transactions and 500 direct-cost postings; no cleanup ran.");
    }

    private async Task EnsureInventoryFixtureAsync(Guid tenantId, Guid farmId, string userId, string label)
    {
        await using ApplicationDbContext context = Context();
        string postingPrefix = "AUTOTEST-P8A-inventory-";
        int existingMovements = await context.StockMovements.CountAsync(x =>
            x.TenantId == tenantId && x.FarmId == farmId && x.PostingIdentity.StartsWith(postingPrefix));
        if (existingMovements > 0)
        {
            existingMovements.ShouldBe(3000, "The labelled inventory performance fixture must not be partially populated.");
            return;
        }
        Guid storeId = await context.Stores.Where(x => x.FarmId == farmId).Select(x => x.Id).SingleAsync();
        UnitOfMeasure? unit = await context.UnitOfMeasures.SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.Code == "P8APERF");
        InventoryItem item;
        StockPosition position;
        Supplier supplier;
        if (unit is null)
        {
            unit = UnitOfMeasure.Create(tenantId, "P8APERF", "AUTOTEST-P8A synthetic unit", "Mass", 6);
            item = InventoryItem.Create(tenantId, farmId, "P8APERF",
                "AUTOTEST-P8A synthetic item", InventoryItemCategory.Other, unit, null,
                LotTrackingPolicy.None, ExpiryPolicy.None);
            position = StockPosition.Create(tenantId, farmId, storeId, item.Id, null);
            supplier = Supplier.Create(tenantId, farmId, "P8APERF",
                "AUTOTEST-P8A synthetic supplier", null);
            context.UnitOfMeasures.Add(unit);
            context.InventoryItems.Add(item);
            context.StockPositions.Add(position);
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();
        }
        else
        {
            item = await context.InventoryItems.SingleAsync(x => x.TenantId == tenantId &&
                x.FarmId == farmId && x.Code == "P8APERF");
            position = await context.StockPositions.SingleAsync(x => x.TenantId == tenantId &&
                x.FarmId == farmId && x.StoreId == storeId && x.InventoryItemId == item.Id);
            supplier = await context.Suppliers.SingleAsync(x => x.TenantId == tenantId &&
                x.FarmId == farmId && x.Code == "P8APERF");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var receipt = StockReceipt.Create(tenantId, farmId, storeId, StockReceiptType.Purchase,
            supplier.Id, new DateOnly(2041, 1, 1), null, $"{label}-inventory",
            null, "Synthetic performance fixture", 0);
        List<StockMovement> movements = [];
        for (int index = 0; index < 3000; index++)
        {
            StockReceiptLine line = receipt.AddLine(item, null, 1m, 2m, receipt.Version);
            movements.Add(StockMovement.CreateReceipt(tenantId, farmId, storeId, position.Id,
                line, StockReceiptType.Purchase, receipt.ReceiptDate, now.AddTicks(index), userId,
                null, $"{postingPrefix}{index:D5}"));
        }
        receipt.MarkPosted(now, userId, $"AUTOTEST-P8A-inventory-post-{receipt.Id:N}", receipt.Version);
        context.StockReceipts.Add(receipt);
        context.StockMovements.AddRange(movements);
        await context.SaveChangesAsync();
        TestContext.Progress.WriteLine("Extended only the labelled fixture with 3,000 inventory movements from one synthetic posted receipt; no cleanup ran.");
    }

    private async Task EnsurePayrollFixtureAsync(Guid tenantId, Guid farmId, string userId)
    {
        await using ApplicationDbContext context = Context();
        int existingEarnings = await context.PayrollEarningLines.CountAsync(x =>
            x.TenantId == tenantId && x.FarmId == farmId &&
            x.SourceFingerprint.StartsWith("AUTOTEST-P8A-payroll-"));
        if (existingEarnings > 0)
        {
            existingEarnings.ShouldBe(1000, "The labelled payroll performance fixture must not be partially populated.");
            return;
        }

        DateOnly start = new(2041, 1, 1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid supervisorId = await context.Persons.Where(x => x.FarmId == farmId &&
            x.DisplayName == "AUTOTEST-P8A supervisor").Select(x => x.Id).SingleAsync();
        Guid[] workerIds = await context.WorkerProfiles.Where(x => x.TenantId == tenantId &&
            x.FarmId == farmId).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        workerIds.Length.ShouldBe(250);
        Dictionary<Guid, Guid> activitiesByField = await context.Activities.Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId).GroupBy(x => x.FieldId)
            .Select(group => new { FieldId = group.Key, ActivityId = group.OrderBy(x => x.Id).Select(x => x.Id).First() })
            .ToDictionaryAsync(x => x.FieldId, x => x.ActivityId);

        List<WorkRecord> workRecords = await context.WorkRecords.Include(x => x.Verification)
            .Where(x => x.TenantId == tenantId && x.FarmId == farmId &&
                x.WorkDate >= start && x.WorkDate < start.AddMonths(1)).ToListAsync();
        if (workRecords.Count == 0)
        {
            var attendances = await context.Attendances.Where(x => x.TenantId == tenantId &&
                x.FarmId == farmId && x.WorkDate >= start && x.WorkDate < start.AddMonths(1))
                .OrderBy(x => x.WorkerProfileId).ThenBy(x => x.WorkDate).ToListAsync();
            foreach (Guid workerId in workerIds)
            {
                WorkerRate rate = WorkerRate.Create(tenantId, farmId, workerId, PayBasis.Daily,
                    null, 10m, start, null);
                context.WorkerRates.Add(rate);
                foreach (Attendance attendance in attendances.Where(x => x.WorkerProfileId == workerId).Take(4))
                {
                    WorkRecord work = WorkRecord.Create(tenantId, farmId, attendance.Id, workerId,
                        attendance.FieldId!.Value, attendance.WorkDate, rate, null,
                        [activitiesByField[attendance.FieldId.Value]], now, userId,
                        "Synthetic performance fixture", 31);
                    work.RecordSupervisorVerification(supervisorId, now, userId, work.Version);
                    work.Confirm(now, userId, work.Version);
                    context.WorkRecords.Add(work);
                    workRecords.Add(work);
                }
            }
            workRecords.Count.ShouldBe(1000);
            await context.SaveChangesAsync();
        }
        else
        {
            workRecords.Count.ShouldBe(1000, "The labelled payroll work fixture must not be partially populated.");
        }

        Dictionary<Guid, Attendance> attendanceById = await context.Attendances.Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && workRecords.Select(w => w.AttendanceId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        Dictionary<Guid, WorkerRate> rateById = await context.WorkerRates.Where(x =>
            x.TenantId == tenantId && x.FarmId == farmId && workRecords.Select(w => w.WorkerRateId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var period = PayrollPeriod.Create(tenantId, farmId, 2041, 1, now, userId, null);
        period.Open(now, userId, null, period.Version);
        var run = PayrollRun.Create(tenantId, farmId, period.Id, now, userId, null);
        int calculationVersion = run.RecordCalculation(run.Version);
        Guid calculationId = Guid.NewGuid();
        var workerLines = new List<PayrollWorkerLine>();
        foreach (IGrouping<Guid, WorkRecord> workerGroup in workRecords.GroupBy(x => x.WorkerProfileId))
        {
            Guid workerLineId = Guid.NewGuid();
            PayrollEarningLine[] earnings = workerGroup.OrderBy(x => x.WorkDate).Select(work =>
            {
                Attendance attendance = attendanceById[work.AttendanceId];
                WorkerRate rate = rateById[work.WorkerRateId];
                return PayrollEarningLine.Create(workerLineId, calculationId, tenantId, farmId,
                    workerGroup.Key, work.Id, "WorkRecord", work.WorkDate, attendance.Id,
                    attendance.Version, work.Verification!.SupervisorVerifiedAt,
                    work.Verification.ManagerConfirmedAt!.Value, work.FieldId, "[]", 1m,
                    "day", "Daily", rate.RateUsd, rate.Id, rate.Version,
                    $"AUTOTEST-P8A-payroll-{work.Id:N}");
            }).ToArray();
            workerLines.Add(PayrollWorkerLine.Create(workerLineId, calculationId, tenantId, farmId,
                workerGroup.Key, "AUTOTEST-P8A synthetic worker", earnings, []));
        }
        var calculation = PayrollCalculation.Create(calculationId, run.Id, period.Id, tenantId,
            farmId, calculationVersion, workerLines, [], "AUTOTEST-P8A-PAYROLL", now, userId, null);
        context.PayrollPeriods.Add(period);
        context.PayrollRuns.Add(run);
        context.PayrollCalculations.Add(calculation);
        await context.SaveChangesAsync();
        TestContext.Progress.WriteLine("Extended only the labelled fixture with 1,000 confirmed work/payroll earning lines; no cleanup ran.");
    }

    private async Task SeedAsync(string label)
    {
        string userId = $"p8a-perf-{Guid.NewGuid():N}";
        DateOnly start = new(2041, 1, 1);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Tenant tenant = Tenant.CreateForGrower(userId, label, null);
        Farm farm = tenant.CreateFarm("P8A-PERF", label, "Synthetic address",
            "Synthetic location", "Other", 500m, "Synthetic irrigation");
        CropVariety variety = tenant.AddCropVariety("P8A", "Synthetic cane");
        ActivityType type = tenant.AddActivityType("P8A", "Synthetic work", true, true, ActivityQuantityBasis.None);
        var supervisor = farm.AddPerson("AUTOTEST-P8A supervisor", null, start);
        List<Field> fields = [];
        for (int index = 0; index < 50; index++)
        {
            Field field = farm.AddField($"F{index:D3}", $"AUTOTEST-P8A field {index}", 10m,
                null, ReportingAreaSource.Declared, "Synthetic", null);
            fields.Add(field);
            CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null,
                variety, variety.Name, start, start.AddMonths(10), start.AddMonths(12), 100m, now, userId);
            field.ActivateCropCycle(cycle, now, userId);
            for (int activity = 0; activity < 40; activity++)
                cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type,
                    ActivityPlanningKind.Planned, start.AddDays(activity), supervisor.Id);
            field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
                start.AddYears(2), start.AddYears(2).AddMonths(10), start.AddYears(3), 100m, now, userId);
        }
        await using ApplicationDbContext context = Context();
        context.Users.Add(new ApplicationUser { Id = userId, UserName = $"{userId}@invalid.example",
            NormalizedUserName = $"{userId}@invalid.example".ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") });
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        for (int index = 0; index < 250; index++)
        {
            var person = farm.AddPerson($"AUTOTEST-P8A worker {index:D3}", null, start);
            WorkerProfile worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id,
                person.Id, EmploymentType.Casual, start, RandomNumberGenerator.GetBytes(16),
                RandomNumberGenerator.GetBytes(12), RandomNumberGenerator.GetBytes(16),
                "AUTOTEST-P8A-unavailable-key", RandomNumberGenerator.GetBytes(32), "SYNTHETIC-MASK");
            context.WorkerProfiles.Add(worker);
            for (int day = 0; day < 20; day++)
                context.Attendances.Add(Attendance.Create(tenant.Id, farm.Id, worker.Id,
                    start.AddDays(day), AttendanceStatus.Present, fields[index % fields.Count].Id,
                    new DateTimeOffset(2041, 2, 1, 0, 0, 0, TimeSpan.Zero), userId, "Synthetic performance fixture", 31));
        }
        await context.SaveChangesAsync();
        Mill mill = Mill.Create(tenant.Id, farm.Id, "P8A", "AUTOTEST-P8A Mill", null, userId, now);
        context.Mills.Add(mill);
        await context.SaveChangesAsync();
        for (int index = 0; index < 2000; index++)
        {
            WeighbridgeTicket ticket = WeighbridgeTicket.CreateDraft(tenant.Id, farm.Id, mill.Id,
                $"AUTOTEST-P8A-T-{index:D5}", start.AddDays(index % 200), 20m, 5m, 15m,
                null, null, label, null, userId, now);
            ticket.Record(userId, now, $"AUTOTEST-P8A-record-{index:D5}", ticket.Version);
            context.WeighbridgeTickets.Add(ticket);
        }
        for (int index = 0; index < 100; index++)
            context.GrowerStatements.Add(GrowerStatement.CreateDraft(tenant.Id, farm.Id, mill.Id,
                $"AUTOTEST-P8A-S-{index:D4}", start.AddDays(index), start.AddDays(index + 30),
                300m, 12000m, "Synthetic performance fixture", userId, now));
        for (int index = 0; index < 10000; index++)
            context.AuditEvents.Add(AuditEvent.Create(tenant.Id, farm.Id, "SyntheticPerformance",
                farm.Id, "FixtureGenerated", userId, TenantSecurityRoles.Grower, null,
                now.AddTicks(index), label, null, "AUTOTEST-P8A synthetic event"));
        await context.SaveChangesAsync();
    }

    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(_connectionString).Options);

    private sealed class SyntheticUser(string id) : IUser
    {
        public string Id => id;
        public List<string> Roles => [TenantSecurityRoles.Grower];
        public string CorrelationId => "AUTOTEST-P8A-performance";
    }

    private sealed class UnusedEvidenceStorage : IEvidenceDocumentStorage
    {
        public Task<StoredEvidence> SaveAsync(Stream source, string fileName, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Performance reads must not write evidence.");
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Performance reads must not transfer evidence.");
    }

    private sealed class UnusedPayrollProjection : IPayrollCostProjectionService
    {
        public Task<PayrollCostReconciliationDto> ProjectAsync(Tenant tenant, Farm farm,
            Cane360.Domain.Payroll.PayrollRun run, Cane360.Domain.Payroll.PayrollCalculation calculation, IUser user,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Performance reads must not project payroll costs.");
        public Task<PayrollCostReconciliationDto> ReconcileAsync(Tenant tenant, Farm farm, IUser user,
            CancellationToken cancellationToken) => throw new InvalidOperationException("Performance reads must not reconcile payroll costs.");
    }
}
