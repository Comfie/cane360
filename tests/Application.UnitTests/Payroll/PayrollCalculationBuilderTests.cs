using System.Text.Json;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollCalculationBuilderTests
{
    private static readonly DateTimeOffset Now = new(2036, 8, 28, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task DailyHectareAndStandardLineUseAuthoritativeSnapshotsAndIncludeEveryEligibleEvidence()
    {
        var setup = CalculationSetup.Create();
        setup.AddEvidence(PayBasis.Daily, 10.005m, null, new DateOnly(2036, 8, 2));
        setup.AddEvidence(PayBasis.Hectare, 3.335m, 1.5m, new DateOnly(2036, 8, 3));
        setup.AddEvidence(PayBasis.StandardLine, 0.335m, 3m, new DateOnly(2036, 8, 4));

        PayrollCalculation calculation = await setup.BuildAsync(1);

        calculation.EvidenceCount.ShouldBe(3);
        calculation.WorkerLines.SelectMany(line => line.EarningLines).Select(line => line.EvidenceId)
            .ShouldBe(setup.Records.Select(record => record.Id), ignoreOrder: true);
        calculation.WorkerLines.SelectMany(line => line.EarningLines).Single(line => line.RateType == "Daily").EarningAmountUsd.ShouldBe(10.01m);
        calculation.WorkerLines.SelectMany(line => line.EarningLines).Single(line => line.RateType == "Hectare").EarningAmountUsd.ShouldBe(5.00m);
        calculation.WorkerLines.SelectMany(line => line.EarningLines).Single(line => line.RateType == "StandardLine").EarningAmountUsd.ShouldBe(1.01m);
        calculation.GrossAmountUsd.ShouldBe(16.02m);
        calculation.DeductionAmountUsd.ShouldBe(0m);
        calculation.NetAmountUsd.ShouldBe(16.02m);
        calculation.WorkerLines.SelectMany(line => line.EarningLines).Select(line => line.RateAmountUsd)
            .ShouldBe([10.005m, 3.335m, 0.335m], ignoreOrder: true);
        JsonSerializer.Deserialize<string[]>(calculation.BlockerSnapshot).ShouldBeEmpty();
    }

    [Test]
    public async Task LaterCurrentRateDoesNotReplaceTheWorkRecordRateSnapshot()
    {
        var setup = CalculationSetup.Create();
        WorkRecord evidence = setup.AddEvidence(PayBasis.Daily, 12.345m, null, new DateOnly(2036, 8, 2));
        setup.AddCurrentRateFor(evidence.WorkerProfileId, PayBasis.Daily, 99m, new DateOnly(2036, 8, 3));

        PayrollEarningLine line = (await setup.BuildAsync(1)).WorkerLines.Single().EarningLines.Single();

        line.RateSourceId.ShouldBe(evidence.WorkerRateId);
        line.RateAmountUsd.ShouldBe(12.345m);
        line.EarningAmountUsd.ShouldBe(12.35m);
    }

    [Test]
    public async Task MonthlyEvidenceProducesStableBlockerAndCannotBecomeAnEarningLine()
    {
        var setup = CalculationSetup.Create();
        setup.AddEvidence(PayBasis.Monthly, 400m, null, new DateOnly(2036, 8, 2));

        PayrollCalculation calculation = await setup.BuildAsync(1);
        string[] blockers = JsonSerializer.Deserialize<string[]>(calculation.BlockerSnapshot)!;

        blockers.ShouldContain(PayrollPreflightBlockerCodes.MonthlyProrationNotConfigured);
        blockers.ShouldContain(PayrollPreflightBlockerCodes.PayrollCalculationIncomplete);
        calculation.EvidenceCount.ShouldBe(0);
        calculation.GrossAmountUsd.ShouldBe(0m);
    }

    [Test]
    public async Task RebuildingTheSameSourcesCreatesASeparateImmutableCalculationVersion()
    {
        var setup = CalculationSetup.Create();
        setup.AddEvidence(PayBasis.Daily, 20m, null, new DateOnly(2036, 8, 2));

        PayrollCalculation first = await setup.BuildAsync(1);
        PayrollCalculation second = await setup.BuildAsync(2);

        first.Id.ShouldNotBe(second.Id);
        first.CalculationVersion.ShouldBe(1);
        second.CalculationVersion.ShouldBe(2);
        first.SourceFingerprint.ShouldBe(second.SourceFingerprint);
        first.GrossAmountUsd.ShouldBe(second.GrossAmountUsd);
    }

    private sealed class CalculationSetup
    {
        private readonly Tenant _tenant;
        private readonly Farm _farm;
        private readonly PayrollPeriod _period;
        private readonly PayrollRun _run;
        private readonly Guid _supervisorId;
        private readonly List<WorkerProfile> _workers = [];
        private readonly List<WorkerRate> _rates = [];
        private readonly List<Attendance> _attendance = [];
        private readonly List<WorkRecord> _records = [];
        private readonly List<Activity> _activities = [];

        private CalculationSetup(Tenant tenant, Farm farm, PayrollPeriod period, PayrollRun run, Guid supervisorId)
        {
            _tenant = tenant;
            _farm = farm;
            _period = period;
            _run = run;
            _supervisorId = supervisorId;
        }

        public IReadOnlyList<WorkRecord> Records => _records;

        public static CalculationSetup Create()
        {
            var tenant = Tenant.CreateForGrower("grower", "Grower", null);
            var variety = tenant.AddCropVariety("P6B", "Synthetic cane");
            var farm = tenant.CreateFarm("P6B", "Payroll farm", "Address", "Location", "Lease", 10m, "Furrow");
            var manager = farm.AddPerson("Manager", null, new DateOnly(2036, 1, 1));
            var supervisor = farm.AddPerson("Supervisor", null, new DateOnly(2036, 1, 1));
            farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2036, 1, 1));
            farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2036, 1, 1));
            tenant.AddFarmManagerMembership("manager", manager.Id);
            var field = farm.AddField("A1", "North", 10m, null, ReportingAreaSource.Declared, "Furrow", null);
            var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
                new DateOnly(2036, 1, 1), new DateOnly(2036, 12, 1), new DateOnly(2037, 1, 31), 100m, Now, "manager");
            field.ActivateCropCycle(cycle, Now, "manager");
            var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2036, 8, Now, "manager", manager.Id);
            period.Open(Now, "manager", manager.Id, period.Version);
            var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, Now, "manager", manager.Id);
            return new CalculationSetup(tenant, farm, period, run, supervisor.Id);
        }

        public WorkRecord AddEvidence(PayBasis basis, decimal rateAmount, decimal? quantity, DateOnly workDate)
        {
            var field = _farm.Fields.Single();
            var cycle = field.CropCycles.Single();
            var quantityBasis = basis switch
            {
                PayBasis.Hectare => ActivityQuantityBasis.Hectares,
                PayBasis.StandardLine => ActivityQuantityBasis.StandardLines,
                _ => ActivityQuantityBasis.None
            };
            var type = _tenant.AddActivityType($"A{_activities.Count}", $"Activity {_activities.Count}", true, true, quantityBasis);
            var activity = cycle.CreateActivity(_tenant.Id, _farm.Id, field.Id, type, ActivityPlanningKind.Planned, workDate, _supervisorId);
            _activities.Add(activity);
            var person = _farm.AddPerson($"Worker {_workers.Count}", null, new DateOnly(2036, 1, 1));
            var worker = WorkerProfile.Create(Guid.NewGuid(), _tenant.Id, _farm.Id, person.Id, EmploymentType.Seasonal,
                new DateOnly(2036, 1, 1), [1], new byte[12], new byte[16], "test-key", new byte[32], "***1234");
            var rate = WorkerRate.Create(_tenant.Id, _farm.Id, worker.Id, basis,
                basis is PayBasis.Hectare or PayBasis.StandardLine ? type.Id : null, rateAmount, new DateOnly(2036, 1, 1), null);
            var attendance = Attendance.Create(_tenant.Id, _farm.Id, worker.Id, workDate, AttendanceStatus.Present,
                field.Id, Now, "manager", null, 0);
            var record = WorkRecord.Create(_tenant.Id, _farm.Id, attendance.Id, worker.Id, field.Id, workDate,
                rate, quantity, [activity.Id], Now, "manager", null, 0);
            record.RecordSupervisorVerification(_supervisorId, Now, "manager", record.Version);
            record.Confirm(Now, "manager", record.Version);
            _workers.Add(worker);
            _rates.Add(rate);
            _attendance.Add(attendance);
            _records.Add(record);
            return record;
        }

        public void AddCurrentRateFor(Guid workerId, PayBasis basis, decimal amount, DateOnly effectiveFrom) =>
            _rates.Add(WorkerRate.Create(_tenant.Id, _farm.Id, workerId, basis, null, amount, effectiveFrom, null));

        public Task<PayrollCalculation> BuildAsync(int version)
        {
            var farms = new Mock<IFarmSetupRepository>();
            var labour = new Mock<ILabourRepository>();
            labour.Setup(repository => repository.GetWorkRecordsAsync(_tenant.Id, _farm.Id, null, null, null, false, It.IsAny<CancellationToken>())).ReturnsAsync(_records);
            labour.Setup(repository => repository.GetWorkersAsync(_tenant.Id, _farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(_workers);
            labour.Setup(repository => repository.GetAttendanceAsync(_tenant.Id, _farm.Id, It.IsAny<Guid>(), It.IsAny<DateOnly>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, Guid _, Guid workerId, DateOnly date, bool _, CancellationToken _) => _attendance.Single(item => item.WorkerProfileId == workerId && item.WorkDate == date));
            labour.Setup(repository => repository.GetRatesAsync(_tenant.Id, _farm.Id, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, Guid _, Guid workerId, bool _, CancellationToken _) => _rates.Where(item => item.WorkerProfileId == workerId).ToArray());
            var payroll = new Mock<IPayrollRepository>();
            payroll.Setup(repository => repository.GetConsumedEvidenceIdsAsync(_tenant.Id, _farm.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<Guid>());
            payroll.Setup(repository => repository.GetPeriodsAsync(_tenant.Id, _farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([_period]);
            payroll.Setup(repository => repository.GetAdvancesAsync(_tenant.Id, _farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            payroll.Setup(repository => repository.GetRecoveriesAsync(_tenant.Id, _farm.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            return PayrollCalculationBuilder.BuildAsync(farms.Object, labour.Object, payroll.Object, _tenant, _farm, _period, _run, version, Now, "manager", null, CancellationToken.None);
        }
    }
}
