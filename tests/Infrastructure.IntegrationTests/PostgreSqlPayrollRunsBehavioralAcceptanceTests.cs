using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only after AddPayrollRunsCalculationAndApproval is explicitly approved and applied to Railway Development.")]
[Category("Phase6BPostMigration")]
[NonParallelizable]
public sealed class PostgreSqlPayrollRunsBehavioralAcceptanceTests
{
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public void Configure()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        _connectionString = LoadConfiguredConnectionString();
    }

    [Test]
    public async Task Phase6BBehaviorCreatesOpenRunAndCalculatesDailyHectareStandardLineWithExactRoundingAndReconciliation()
    {
        Scenario scenario = await SeedAsync([
            new EvidenceSpec(PayBasis.Daily, 10.005m, null, 2),
            new EvidenceSpec(PayBasis.Hectare, 3.335m, 1.5m, 3),
            new EvidenceSpec(PayBasis.StandardLine, 0.335m, 3m, 4)
        ]);

        PayrollRunDto created = await CreateRunAsync(scenario);
        PayrollRunDto calculated = await CalculateAsync(scenario, created);

        created.PeriodStatus.ShouldBe("Open");
        calculated.Status.ShouldBe("Calculated");
        PayrollCalculationDto result = calculated.Calculation.ShouldNotBeNull();
        result.EvidenceCount.ShouldBe(3);
        result.WorkerCount.ShouldBe(1);
        result.Workers.Single().Earnings.Select(line => line.EvidenceId).ShouldBe(scenario.EvidenceIds, ignoreOrder: true);
        result.Workers.Single().Earnings.Single(line => line.RateType == "Daily").EarningAmountUsd.ShouldBe(10.01m);
        result.Workers.Single().Earnings.Single(line => line.RateType == "Hectare").EarningAmountUsd.ShouldBe(5.00m);
        result.Workers.Single().Earnings.Single(line => line.RateType == "StandardLine").EarningAmountUsd.ShouldBe(1.01m);
        result.GrossAmountUsd.ShouldBe(16.02m);
        result.DeductionAmountUsd.ShouldBe(0m);
        result.NetAmountUsd.ShouldBe(16.02m);
        result.Workers.Sum(line => line.GrossAmountUsd).ShouldBe(result.GrossAmountUsd);
    }

    [Test]
    public async Task Phase6BBehaviorMonthlyEvidenceBlocksSubmissionWithoutSilentOmission()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Monthly, 400m, null, 2)]);
        PayrollRunDto calculated = await CalculateAsync(scenario, await CreateRunAsync(scenario));

        PayrollCalculationDto monthly = calculated.Calculation.ShouldNotBeNull();
        monthly.BlockerCodes.ShouldContain(PayrollPreflightBlockerCodes.MonthlyProrationNotConfigured);
        monthly.EvidenceCount.ShouldBe(0);
        ConflictException exception = await Should.ThrowAsync<ConflictException>(() => SubmitAsync(scenario, calculated));
        exception.Message.ShouldContain(PayrollPreflightBlockerCodes.MonthlyProrationNotConfigured);
    }

    [Test]
    public async Task Phase6BBehaviorRecalculationCreatesANewImmutableVersionAndOldRowsRejectMutation()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 25m, null, 2)]);
        PayrollRunDto first = await CalculateAsync(scenario, await CreateRunAsync(scenario));
        PayrollRunDto second = await CalculateAsync(scenario, first);

        second.LatestCalculationVersion.ShouldBe(2);
        await using var context = Context();
        PayrollCalculation[] calculations = await context.PayrollCalculations.AsNoTracking().Where(item => item.PayrollRunId == second.Id).OrderBy(item => item.CalculationVersion).ToArrayAsync();
        calculations.Select(item => item.CalculationVersion).ShouldBe([1, 2]);
        calculations[0].Id.ShouldNotBe(calculations[1].Id);
        await AssertDirectMutationRejectedAsync("PayrollCalculations", calculations[0].Id, "UPDATE");
    }

    [TestCase("Attendances", "labour", "\"Version\" = \"Version\" + 1", PayrollPreflightBlockerCodes.EvidenceChangedAfterCalculation)]
    [TestCase("WorkVerifications", "labour", "\"ManagerConfirmedAt\" = \"ManagerConfirmedAt\" + interval '1 second'", PayrollPreflightBlockerCodes.VerificationChanged)]
    [TestCase("WorkerRates", "labour", "\"RateUsd\" = \"RateUsd\" + 1, \"Version\" = \"Version\" + 1", PayrollPreflightBlockerCodes.RateSnapshotChanged)]
    public async Task Phase6BBehaviorStaleEvidenceVerificationAndRateReturnConflictAtApproval(string table, string schema, string mutation, string expectedCode)
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 25m, null, 2)]);
        PayrollRunDto pending = await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario)));
        Guid sourceId = table switch
        {
            "Attendances" => scenario.AttendanceIds.Single(),
            "WorkVerifications" => scenario.VerificationIds.Single(),
            _ => scenario.RateIds.Single()
        };
        await ExecuteAsync($"UPDATE {schema}.\"{table}\" SET {mutation} WHERE \"Id\" = @id", ("id", sourceId));

        ConflictException exception = await Should.ThrowAsync<ConflictException>(() => DecideAsync(scenario, pending, true, null, $"{scenario.Label}-stale"));
        exception.Message.ShouldContain(expectedCode);
        await using var context = Context();
        (await context.PayrollApprovals.CountAsync(item => item.PayrollRunId == pending.Id)).ShouldBe(0);
        (await context.PayrollEvidenceConsumptions.CountAsync(item => item.PayrollRunId == pending.Id)).ShouldBe(0);
    }

    [TestCase("Advance", PayrollPreflightBlockerCodes.AdvanceChangedAfterCalculation)]
    [TestCase("Worker", PayrollPreflightBlockerCodes.PayrollCalculationStale)]
    [TestCase("Period", PayrollPreflightBlockerCodes.PayrollPeriodNotOpen)]
    public async Task Phase6BBehaviorAdvanceWorkerAndPeriodChangesReturnConflictAtApproval(string source, string expectedCode)
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)], source == "Advance" ? [new AdvanceSpec(20m, -1)] : null);
        PayrollRunDto pending = await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario)));
        if (source == "Advance")
            await ExecuteAsync("UPDATE payroll.\"WorkerAdvances\" SET \"Status\" = 'Approved', \"Version\" = \"Version\" + 1 WHERE \"Id\" = @id", ("id", scenario.AdvanceIds.Single()));
        else if (source == "Worker")
            await ExecuteAsync("UPDATE labour.\"WorkerProfiles\" SET \"Status\" = 'Archived', \"ActiveTo\" = DATE '2036-08-31', \"Version\" = \"Version\" + 1 WHERE \"Id\" = (SELECT \"WorkerProfileId\" FROM labour.\"WorkRecords\" WHERE \"Id\" = @id)", ("id", scenario.EvidenceIds.Single()));
        else
        {
            await using var context = Context();
            PayrollPeriod period = await context.PayrollPeriods.SingleAsync(item => item.Id == scenario.PeriodId);
            period.Close(DateTimeOffset.UtcNow, scenario.GrowerUserId, null, pending.Id, period.Version);
            await context.SaveChangesAsync();
        }

        ConflictException exception = await Should.ThrowAsync<ConflictException>(() => DecideAsync(scenario, pending, true, null, $"{scenario.Label}-{source}"));
        exception.Message.ShouldContain(expectedCode);
    }

    [Test]
    public async Task Phase6BBehaviorAlreadyConsumedEvidenceBlocksRecalculationAndSubmission()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)]);
        PayrollRunDto first = await CalculateAsync(scenario, await CreateRunAsync(scenario));
        await ExecuteAsync("INSERT INTO payroll.\"PayrollEvidenceConsumptions\" (\"Id\", \"PayrollRunId\", \"PayrollCalculationId\", \"TenantId\", \"FarmId\", \"EvidenceId\", \"ConsumedAt\") VALUES (@id, @run, @calculation, @tenant, @farm, @evidence, now())",
            ("id", Guid.NewGuid()), ("run", first.Id), ("calculation", first.Calculation!.Id), ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("evidence", scenario.EvidenceIds.Single()));
        PayrollRunDto recalculated = await CalculateAsync(scenario, first);

        recalculated.Calculation!.BlockerCodes.ShouldContain(PayrollPreflightBlockerCodes.EvidenceAlreadyConsumedByPayroll);
        await Should.ThrowAsync<ConflictException>(() => SubmitAsync(scenario, recalculated));
    }

    [Test]
    public async Task Phase6BBehaviorExactGrowerApprovalConsumesOnceClosesPeriodAndIdempotentRetryCreatesNoDuplicatesOrSideEffects()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)]);
        PayrollRunDto pending = await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario)));
        await Should.ThrowAsync<ForbiddenAccessException>(() => DecideAsync(scenario, pending, true, null, $"{scenario.Label}-manager", scenario.ManagerUserId));
        await Should.ThrowAsync<ConflictException>(() => DecideAsync(scenario, pending with { Version = pending.Version - 1 }, true, null, $"{scenario.Label}-wrong-version"));
        await Should.ThrowAsync<NotFoundException>(() => DecideAsync(scenario, pending with { SubmittedCalculationVersion = pending.SubmittedCalculationVersion + 1 }, true, null, $"{scenario.Label}-wrong-calculation"));
        string key = $"{scenario.Label}-approve";
        PayrollRunDto approved = await DecideAsync(scenario, pending, true, null, key);
        PayrollRunDto retried = await DecideAsync(scenario, pending, true, null, key);

        approved.Status.ShouldBe("Approved");
        retried.Status.ShouldBe("Approved");
        approved.PeriodStatus.ShouldBe("Closed");
        await using var context = Context();
        (await context.PayrollApprovals.CountAsync(item => item.PayrollRunId == approved.Id)).ShouldBe(1);
        (await context.PayrollEvidenceConsumptions.CountAsync(item => item.PayrollRunId == approved.Id)).ShouldBe(1);
        (await context.AuditEvents.CountAsync(item => item.TenantId == scenario.TenantId && item.FarmId == scenario.FarmId && item.SubjectId == approved.Id && item.Action == "PayrollApproved")).ShouldBe(1);
        PayrollPeriod period = await context.PayrollPeriods.SingleAsync(item => item.Id == scenario.PeriodId);
        period.Status.ShouldBe(PayrollPeriodStatus.Closed);
        period.ClosedByPayrollRunId.ShouldBe(approved.Id);
        (await context.OperationalCostPostings.CountAsync(item => item.TenantId == scenario.TenantId && item.FarmId == scenario.FarmId)).ShouldBe(0);
        (await CountAsync("SELECT count(*) FROM information_schema.tables WHERE table_schema = 'payroll' AND (table_name ILIKE '%payment%' OR table_name ILIKE '%payslip%' OR table_name ILIKE '%tax%' OR table_name ILIKE '%pension%')")).ShouldBe(0);
    }

    [Test]
    public async Task Phase6BBehaviorRejectedDecisionCreatesNoConsumptionOrRecoveryAndRequiresRecalculation()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)], [new AdvanceSpec(20m, -2)]);
        PayrollRunDto pending = await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario)));
        PayrollRunDto rejected = await DecideAsync(scenario, pending, false, "Source review failed", $"{scenario.Label}-reject");

        rejected.Status.ShouldBe("Rejected");
        rejected.RejectionReason.ShouldBe("Source review failed");
        await using var context = Context();
        (await context.PayrollApprovals.CountAsync(item => item.PayrollRunId == rejected.Id && !item.Approved)).ShouldBe(1);
        (await context.PayrollEvidenceConsumptions.CountAsync(item => item.PayrollRunId == rejected.Id)).ShouldBe(0);
        (await context.AdvanceRecoveries.CountAsync(item => item.PayrollRunId == rejected.Id)).ShouldBe(0);
        PayrollRunDto recalculated = await CalculateAsync(scenario, rejected);
        recalculated.LatestCalculationVersion.ShouldBe(2);
    }

    [Test]
    public async Task Phase6BBehaviorConcurrentApprovalProducesOneAuthoritativeApprovalConsumptionAuditAndTimelineOutcome()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)]);
        PayrollRunDto pending = await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario)));
        Task<PayrollRunDto>[] attempts = Enumerable.Range(0, 2).Select(index => DecideAsync(scenario, pending, true, null, $"{scenario.Label}-concurrent-{index}")).ToArray();
        try { await Task.WhenAll(attempts); } catch (Exception) { }

        attempts.Count(task => task.Status == TaskStatus.RanToCompletion).ShouldBe(1);
        await using var context = Context();
        (await context.PayrollApprovals.CountAsync(item => item.PayrollRunId == pending.Id)).ShouldBe(1);
        (await context.PayrollEvidenceConsumptions.CountAsync(item => item.PayrollRunId == pending.Id)).ShouldBe(1);
        (await context.PayrollAuditEventLinks.CountAsync(item => item.PayrollApprovalId != null && item.PayrollRunId == null && item.TenantId == scenario.TenantId && item.FarmId == scenario.FarmId)).ShouldBe(1);
    }

    [Test]
    public async Task Phase6BBehaviorConcurrentPayrollConsumptionAttemptsCanConsumeEvidenceOnlyOnce()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)]);
        PayrollRunDto calculated = await CalculateAsync(scenario, await CreateRunAsync(scenario));
        Guid calculationId = calculated.Calculation!.Id;
        async Task InsertAsync() => await ExecuteAsync("INSERT INTO payroll.\"PayrollEvidenceConsumptions\" (\"Id\", \"PayrollRunId\", \"PayrollCalculationId\", \"TenantId\", \"FarmId\", \"EvidenceId\", \"ConsumedAt\") VALUES (@id, @run, @calculation, @tenant, @farm, @evidence, now())",
            ("id", Guid.NewGuid()), ("run", calculated.Id), ("calculation", calculationId), ("tenant", scenario.TenantId), ("farm", scenario.FarmId), ("evidence", scenario.EvidenceIds.Single()));
        Task[] attempts = [InsertAsync(), InsertAsync()];
        try { await Task.WhenAll(attempts); } catch (Exception) { }

        attempts.Count(task => task.Status == TaskStatus.RanToCompletion).ShouldBe(1);
        await using var context = Context();
        (await context.PayrollEvidenceConsumptions.CountAsync(item => item.EvidenceId == scenario.EvidenceIds.Single())).ShouldBe(1);
    }

    [Test]
    public async Task Phase6BBehaviorMultipleAdvancesRecoverDeterministicallyAndPartiallyWithoutNegativeNetAndReconcileOutstandingBalance()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)], [new AdvanceSpec(40m, -2), new AdvanceSpec(40m, -1)]);
        PayrollRunDto calculated = await CalculateAsync(scenario, await CreateRunAsync(scenario));
        PayrollWorkerLineDto worker = calculated.Calculation!.Workers.Single();

        worker.GrossAmountUsd.ShouldBe(50m);
        worker.DeductionAmountUsd.ShouldBe(50m);
        worker.NetAmountUsd.ShouldBe(0m);
        worker.AdvanceDeductions.Count.ShouldBe(2);
        worker.AdvanceDeductions.Single(item => item.WorkerAdvanceId == scenario.AdvanceIds[0]).AmountUsd.ShouldBe(40m);
        worker.AdvanceDeductions.Single(item => item.WorkerAdvanceId == scenario.AdvanceIds[1]).AmountUsd.ShouldBe(10m);
        PayrollRunDto approved = await DecideAsync(scenario, await SubmitAsync(scenario, calculated), true, null, $"{scenario.Label}-advance-approve");
        await using var context = Context();
        AdvanceRecovery[] recoveries = await context.AdvanceRecoveries.AsNoTracking().Where(item => item.PayrollRunId == approved.Id).OrderBy(item => item.RecoveredAt).ThenBy(item => item.WorkerAdvanceId).ToArrayAsync();
        recoveries.Sum(item => item.AmountUsd).ShouldBe(50m);
        decimal firstOutstanding = 40m - recoveries.Where(item => item.WorkerAdvanceId == scenario.AdvanceIds[0]).Sum(item => item.AmountUsd);
        decimal secondOutstanding = 40m - recoveries.Where(item => item.WorkerAdvanceId == scenario.AdvanceIds[1]).Sum(item => item.AmountUsd);
        firstOutstanding.ShouldBe(0m);
        secondOutstanding.ShouldBe(30m);
    }

    [Test]
    public async Task Phase6BBehaviorCrossTenantAndCrossFarmReferencesAreRejectedByApplicationAndDatabase()
    {
        Scenario first = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 10m, null, 2)]);
        Scenario second = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 10m, null, 2)]);
        await using (var context = Context())
        {
            var handler = new CreatePayrollRunCommandHandler(new FarmSetupRepository(context), new PayrollRepository(context), new AcceptanceUser(first.ManagerUserId), TimeProvider.System);
            await Should.ThrowAsync<NotFoundException>(() => handler.Handle(new CreatePayrollRunCommand(second.PeriodId), CancellationToken.None));
        }
        PayrollRunDto calculated = await CalculateAsync(first, await CreateRunAsync(first));
        NpgsqlException exception = await Should.ThrowAsync<NpgsqlException>(() => ExecuteAsync("INSERT INTO payroll.\"PayrollEvidenceConsumptions\" (\"Id\", \"PayrollRunId\", \"PayrollCalculationId\", \"TenantId\", \"FarmId\", \"EvidenceId\", \"ConsumedAt\") VALUES (@id, @run, @calculation, @tenant, @farm, @evidence, now())",
            ("id", Guid.NewGuid()), ("run", calculated.Id), ("calculation", calculated.Calculation!.Id), ("tenant", second.TenantId), ("farm", second.FarmId), ("evidence", first.EvidenceIds.Single())));
        exception.ShouldNotBeNull();
    }

    [Test]
    public async Task Phase6BBehaviorEveryImmutablePayrollTableIncludingAuditLinksRejectsDirectUpdateAndDeleteWhileAllowingApprovalInserts()
    {
        Scenario scenario = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 50m, null, 2)], [new AdvanceSpec(20m, -1)]);
        PayrollRunDto approved = await DecideAsync(scenario, await SubmitAsync(scenario, await CalculateAsync(scenario, await CreateRunAsync(scenario))), true, null, $"{scenario.Label}-immutable");
        await using var context = Context();
        var ids = new Dictionary<string, Guid>
        {
            ["PayrollCalculations"] = (await context.PayrollCalculations.AsNoTracking().SingleAsync(item => item.PayrollRunId == approved.Id)).Id,
            ["PayrollWorkerLines"] = (await context.PayrollWorkerLines.AsNoTracking().SingleAsync(item => item.PayrollCalculationId == approved.Calculation!.Id)).Id,
            ["PayrollEarningLines"] = (await context.PayrollEarningLines.AsNoTracking().SingleAsync(item => item.PayrollCalculationId == approved.Calculation!.Id)).Id,
            ["PayrollAdvanceDeductions"] = (await context.PayrollAdvanceDeductions.AsNoTracking().SingleAsync(item => item.PayrollCalculationId == approved.Calculation!.Id)).Id,
            ["PayrollApprovals"] = (await context.PayrollApprovals.AsNoTracking().SingleAsync(item => item.PayrollRunId == approved.Id)).Id,
            ["PayrollEvidenceConsumptions"] = (await context.PayrollEvidenceConsumptions.AsNoTracking().SingleAsync(item => item.PayrollRunId == approved.Id)).Id,
            ["AdvanceRecoveries"] = (await context.AdvanceRecoveries.AsNoTracking().SingleAsync(item => item.PayrollRunId == approved.Id)).Id,
            ["PayrollAuditEventLinks"] = (await context.PayrollAuditEventLinks.AsNoTracking().FirstAsync(item => item.TenantId == scenario.TenantId && item.FarmId == scenario.FarmId && item.PayrollApprovalId != null)).Id
        };
        foreach ((string table, Guid id) in ids)
        {
            await AssertDirectMutationRejectedAsync(table, id, "UPDATE");
            await AssertDirectMutationRejectedAsync(table, id, "DELETE");
        }
        Guid consumedEvidenceId = scenario.EvidenceIds.Single();
        long consumedVersion = await context.WorkRecords.AsNoTracking().Where(item => item.Id == consumedEvidenceId).Select(item => item.Version).SingleAsync();
        await AssertMutationRejectedAsync("UPDATE labour.\"WorkRecords\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = @id", consumedEvidenceId);
        await AssertMutationRejectedAsync("DELETE FROM labour.\"WorkRecords\" WHERE \"Id\" = @id", consumedEvidenceId);
        (await context.WorkRecords.AsNoTracking().Where(item => item.Id == consumedEvidenceId).Select(item => item.Version).SingleAsync()).ShouldBe(consumedVersion);

        Scenario unconsumed = await SeedAsync([new EvidenceSpec(PayBasis.Daily, 25m, null, 3)]);
        unconsumed.TenantId.ShouldNotBe(scenario.TenantId);
        Guid unconsumedEvidenceId = unconsumed.EvidenceIds.Single();
        long originalVersion = await context.WorkRecords.AsNoTracking().Where(item => item.Id == unconsumedEvidenceId).Select(item => item.Version).SingleAsync();
        await ExecuteAsync("UPDATE labour.\"WorkRecords\" SET \"Version\" = \"Version\" + 1 WHERE \"Id\" = @id", ("id", unconsumedEvidenceId));
        (await context.WorkRecords.AsNoTracking().Where(item => item.Id == unconsumedEvidenceId).Select(item => item.Version).SingleAsync()).ShouldBe(originalVersion + 1);
        await ExecuteAsync("DELETE FROM labour.\"WorkVerifications\" WHERE \"WorkRecordId\" = @id", ("id", unconsumedEvidenceId));
        await ExecuteAsync("DELETE FROM labour.\"WorkRecordActivities\" WHERE \"WorkRecordId\" = @id", ("id", unconsumedEvidenceId));
        await ExecuteAsync("DELETE FROM labour.\"WorkScopes\" WHERE \"WorkRecordId\" = @id", ("id", unconsumedEvidenceId));
        await ExecuteAsync("DELETE FROM labour.\"WorkRecords\" WHERE \"Id\" = @id", ("id", unconsumedEvidenceId));
        (await context.WorkRecords.AsNoTracking().AnyAsync(item => item.Id == unconsumedEvidenceId)).ShouldBeFalse();
    }

    private async Task<Scenario> SeedAsync(IReadOnlyList<EvidenceSpec> evidence, IReadOnlyList<AdvanceSpec>? advances = null)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await SeedOnceAsync(evidence, advances);
            }
            catch (Exception exception) when (attempt < 3 && IsTransient(exception))
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }
    }

    private async Task<Scenario> SeedOnceAsync(IReadOnlyList<EvidenceSpec> evidence, IReadOnlyList<AdvanceSpec>? advances)
    {
        string label = $"AUTOTEST-P6B-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        string growerId = $"p6b-grower-{Guid.NewGuid():N}";
        string managerId = $"p6b-manager-{Guid.NewGuid():N}";
        var tenant = Tenant.CreateForGrower(growerId, label, null);
        var variety = tenant.AddCropVariety($"V{Guid.NewGuid():N}"[..20], "Synthetic cane");
        var farm = tenant.CreateFarm($"F{Guid.NewGuid():N}"[..20], label, "Synthetic address", "Railway Development", "Synthetic", 10m, "Synthetic");
        var manager = farm.AddPerson("Synthetic P6B manager", null, new DateOnly(2036, 1, 1));
        var supervisor = farm.AddPerson("Synthetic P6B supervisor", null, new DateOnly(2036, 1, 1));
        var workerPerson = farm.AddPerson("Synthetic P6B worker", null, new DateOnly(2036, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2036, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2036, 1, 1));
        tenant.AddFarmManagerMembership(managerId, manager.Id);
        var field = farm.AddField("P6B", "Synthetic field", 10m, null, ReportingAreaSource.Declared, "Synthetic", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name, new DateOnly(2036, 1, 1), new DateOnly(2036, 12, 1), new DateOnly(2037, 1, 31), 100m, DateTimeOffset.UtcNow, managerId);
        field.ActivateCropCycle(cycle, DateTimeOffset.UtcNow, managerId);
        var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id, EmploymentType.Seasonal, new DateOnly(2036, 1, 1), [1], new byte[12], new byte[16], "synthetic-key", Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray(), "***1234");
        var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2036, 8, DateTimeOffset.UtcNow, managerId, manager.Id);
        period.Open(DateTimeOffset.UtcNow, managerId, manager.Id, period.Version);
        var records = new List<WorkRecord>(); var attendanceRows = new List<Attendance>(); var rates = new List<WorkerRate>();
        foreach ((EvidenceSpec spec, int index) in evidence.Select((item, index) => (item, index)))
        {
            ActivityQuantityBasis quantityBasis = spec.Basis switch { PayBasis.Hectare => ActivityQuantityBasis.Hectares, PayBasis.StandardLine => ActivityQuantityBasis.StandardLines, _ => ActivityQuantityBasis.None };
            var type = tenant.AddActivityType($"T{index}{Guid.NewGuid():N}"[..20], $"Synthetic {spec.Basis} {index}", true, true, quantityBasis);
            DateOnly workDate = new(2036, 8, spec.Day);
            var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type, ActivityPlanningKind.Planned, workDate, supervisor.Id);
            var rate = WorkerRate.Create(tenant.Id, farm.Id, worker.Id, spec.Basis, spec.Basis is PayBasis.Hectare or PayBasis.StandardLine ? type.Id : null, spec.Rate, new DateOnly(2036, 1, 1), null);
            var attendance = Attendance.Create(tenant.Id, farm.Id, worker.Id, workDate, AttendanceStatus.Present, field.Id, DateTimeOffset.UtcNow, managerId, null, 0);
            var record = WorkRecord.Create(tenant.Id, farm.Id, attendance.Id, worker.Id, field.Id, workDate, rate, spec.Quantity, [activity.Id], DateTimeOffset.UtcNow, managerId, null, 0);
            record.RecordSupervisorVerification(supervisor.Id, DateTimeOffset.UtcNow, managerId, record.Version);
            record.Confirm(DateTimeOffset.UtcNow, managerId, record.Version);
            rates.Add(rate); attendanceRows.Add(attendance); records.Add(record);
        }
        var advanceRows = new List<WorkerAdvance>(); var approvalRows = new List<AdvanceApproval>(); var issueRows = new List<AdvanceIssue>();
        foreach (AdvanceSpec spec in advances ?? [])
        {
            DateTimeOffset issuedAt = DateTimeOffset.UtcNow.AddHours(spec.IssueHourOffset);
            var advance = WorkerAdvance.Create(tenant.Id, farm.Id, worker.Id, spec.Amount, "Synthetic recovery", new DateOnly(2036, 7, 1), period.Id, 1, issuedAt.AddDays(-1), managerId, manager.Id);
            advance.SetSchedule([period.Id], advance.Version); advance.Submit(advance.Version); long approvalVersion = advance.Version;
            var approval = AdvanceApproval.Create(advance.Id, tenant.Id, farm.Id, approvalVersion, advance.RequestedAmountUsd, advance.Installments, true, growerId, issuedAt.AddHours(-1), null, $"{label}-advance-approval-{advance.Id:N}");
            advance.Decide(true, advance.Version);
            var issue = AdvanceIssue.Cash(advance.Id, tenant.Id, farm.Id, advance.RequestedAmountUsd, issuedAt, managerId, manager.Id, worker.Id, true, $"{label}-advance-issue-{advance.Id:N}");
            advance.Issue(advance.RequestedAmountUsd, advance.Version);
            advanceRows.Add(advance); approvalRows.Add(approval); issueRows.Add(issue);
        }
        await using var context = Context();
        ApplicationUser growerUser = User(growerId); ApplicationUser managerUser = User(managerId);
        string? smokePassword = Environment.GetEnvironmentVariable("CANE360_P6B_SMOKE_PASSWORD");
        if (!string.IsNullOrWhiteSpace(smokePassword))
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            growerUser.PasswordHash = hasher.HashPassword(growerUser, smokePassword);
            managerUser.PasswordHash = hasher.HashPassword(managerUser, smokePassword);
        }
        context.Users.AddRange(growerUser, managerUser);
        context.Tenants.Add(tenant); context.WorkerProfiles.Add(worker); context.WorkerRates.AddRange(rates); context.Attendances.AddRange(attendanceRows); context.WorkRecords.AddRange(records); context.PayrollPeriods.Add(period); context.WorkerAdvances.AddRange(advanceRows); context.AdvanceApprovals.AddRange(approvalRows); context.AdvanceIssues.AddRange(issueRows);
        await context.SaveChangesAsync();
        string? smokeContextPath = Environment.GetEnvironmentVariable("CANE360_P6B_SMOKE_CONTEXT_PATH");
        if (!string.IsNullOrWhiteSpace(smokePassword) && !string.IsNullOrWhiteSpace(smokeContextPath))
        {
            string contextJson = System.Text.Json.JsonSerializer.Serialize(new { label, tenantId = tenant.Id, farmId = farm.Id, periodId = period.Id, growerEmail = growerUser.Email, managerEmail = managerUser.Email, password = smokePassword });
            await File.AppendAllTextAsync(smokeContextPath, contextJson + Environment.NewLine);
        }
        return new Scenario(label, tenant.Id, farm.Id, period.Id, growerId, managerId, records.Select(item => item.Id).ToArray(), attendanceRows.Select(item => item.Id).ToArray(), records.Select(item => item.Verification!.Id).ToArray(), rates.Select(item => item.Id).ToArray(), advanceRows.Select(item => item.Id).ToArray());
    }

    private static bool IsTransient(Exception exception) =>
        exception is NpgsqlException { IsTransient: true } ||
        exception.InnerException is not null && IsTransient(exception.InnerException);

    private async Task<PayrollRunDto> CreateRunAsync(Scenario scenario)
    { await using var context = Context(); return await new CreatePayrollRunCommandHandler(new FarmSetupRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new CreatePayrollRunCommand(scenario.PeriodId), CancellationToken.None); }
    private async Task<PayrollRunDto> CalculateAsync(Scenario scenario, PayrollRunDto run)
    { await using var context = Context(); return await new CalculatePayrollRunCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new CalculatePayrollRunCommand(run.Id, run.Version), CancellationToken.None); }
    private async Task<PayrollRunDto> SubmitAsync(Scenario scenario, PayrollRunDto run)
    { await using var context = Context(); return await new SubmitPayrollRunCommandHandler(new FarmSetupRepository(context), new PayrollRepository(context), new AcceptanceUser(scenario.ManagerUserId), TimeProvider.System).Handle(new SubmitPayrollRunCommand(run.Id, run.Version, run.LatestCalculationVersion), CancellationToken.None); }
    private async Task<PayrollRunDto> DecideAsync(Scenario scenario, PayrollRunDto run, bool approved, string? reason, string key, string? userId = null)
    { await using var context = Context(); return await new DecidePayrollRunCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), new PayrollRepository(context), new AcceptanceUser(userId ?? scenario.GrowerUserId), TimeProvider.System).Handle(new DecidePayrollRunCommand(run.Id, run.Version, run.SubmittedCalculationVersion ?? run.LatestCalculationVersion, approved, reason, key), CancellationToken.None); }

    private async Task AssertDirectMutationRejectedAsync(string table, Guid id, string operation) => await AssertMutationRejectedAsync(operation == "UPDATE" ? $"UPDATE payroll.\"{table}\" SET \"Id\" = \"Id\" WHERE \"Id\" = @id" : $"DELETE FROM payroll.\"{table}\" WHERE \"Id\" = @id", id);
    private async Task AssertMutationRejectedAsync(string sql, Guid id)
    { NpgsqlException exception = await Should.ThrowAsync<NpgsqlException>(() => ExecuteAsync(sql, ("id", id))); (exception.Message.Contains("append-only", StringComparison.OrdinalIgnoreCase) || exception.Message.Contains("immutable", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue(); }
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach ((string name, object value) in parameters) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
    private async Task<int> CountAsync(string sql)
    { await using var connection = new NpgsqlConnection(_connectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); return Convert.ToInt32(await command.ExecuteScalarAsync()); }
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_connectionString, options => options.CommandTimeout(120)).Options);
    private static string LoadConfiguredConnectionString()
    { string? value = Environment.GetEnvironmentVariable("ConnectionStrings__Cane360Db"); if (!string.IsNullOrWhiteSpace(value)) return value; var config = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build(); return config.GetConnectionString("Cane360Db") ?? throw new InvalidOperationException("The configured Railway development connection is unavailable."); }
    private static ApplicationUser User(string id) => new() { Id = id, UserName = $"{id}@invalid.example", NormalizedUserName = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), Email = $"{id}@invalid.example", NormalizedEmail = $"{id}@INVALID.EXAMPLE".ToUpperInvariant(), SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N") };
    private sealed class AcceptanceUser(string id) : IUser { public string? Id => id; public List<string>? Roles => null; public string? CorrelationId => $"AUTOTEST-P6B-{Guid.NewGuid():N}"; }
    private sealed record EvidenceSpec(PayBasis Basis, decimal Rate, decimal? Quantity, int Day);
    private sealed record AdvanceSpec(decimal Amount, int IssueHourOffset);
    private sealed record Scenario(string Label, Guid TenantId, Guid FarmId, Guid PeriodId, string GrowerUserId, string ManagerUserId, IReadOnlyList<Guid> EvidenceIds, IReadOnlyList<Guid> AttendanceIds, IReadOnlyList<Guid> VerificationIds, IReadOnlyList<Guid> RateIds, IReadOnlyList<Guid> AdvanceIds);
}
