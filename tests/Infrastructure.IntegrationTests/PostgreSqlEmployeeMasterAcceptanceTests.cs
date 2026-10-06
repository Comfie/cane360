using System.Text.Json;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Labour;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cane360.Infrastructure.IntegrationTests;

[TestFixture]
[Explicit("Run only against Railway Development. Each test rolls back only its own labelled synthetic transaction.")]
[Category("CR013Acceptance")]
[NonParallelizable]
public sealed class PostgreSqlEmployeeMasterAcceptanceTests
{
    private ApplicationDbContext _context = null!;
    private IDbContextTransaction _transaction = null!;
    private EmployeeMasterAcceptanceFixture _f = null!;

    [SetUp]
    public async Task SeedOwnSyntheticTransaction()
    {
        _context = EmployeeMasterAcceptanceFixture.Context();
        (await _context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        _transaction = await _context.Database.BeginTransactionAsync();
        _f = new($"AUTOTEST-CR013-{Guid.NewGuid():N}");
        await _f.Seed(_context);
    }

    [TearDown]
    public async Task RollBackOnlyOwnUncommittedWork()
    {
        if (_transaction is not null) {await _transaction.RollbackAsync(); await _transaction.DisposeAsync();}
        if (_context is not null) await _context.DisposeAsync();
    }

    [Test]
    public async Task PreMigrationLabelledWorkerAttendanceEvidenceAndPayrollLinksSurvive()
    {
        string label = Environment.GetEnvironmentVariable("CANE360_CR013_LEGACY_LABEL")
            ?? throw new InvalidOperationException("Supply the uniquely labelled prerequisite fixture.");
        label.ShouldStartWith("AUTOTEST-CR013-LEGACY-");
        var tenant = await _context.Tenants.SingleAsync(item => _context.GrowerProfiles.Any(p => p.TenantId == item.Id && p.DisplayName == label));
        var farm = await _context.Farms.SingleAsync(item => item.TenantId == tenant.Id && item.Name == label);
        WorkerProfile worker = await _context.WorkerProfiles.SingleAsync(item => item.TenantId == tenant.Id && item.FarmId == farm.Id &&
            _context.Persons.Any(p => p.Id == item.PersonId && p.FarmId == farm.Id && p.DisplayName == label));
        worker.EmployeeNumber.ShouldBeNull(); worker.FirstName.ShouldBeNull(); worker.DateOfBirth.ShouldBeNull();
        worker.PhotoReference.ShouldBeNull(); worker.NextOfKinName.ShouldBeNull();
        worker.ActiveFrom.ShouldBe(EmployeeMasterAcceptanceFixture.Date);
        worker.IsActiveOn(EmployeeMasterAcceptanceFixture.Date).ShouldBeTrue();
        Attendance attendance = await _context.Set<Attendance>().SingleAsync(item => item.TenantId == tenant.Id && item.FarmId == farm.Id && item.WorkerProfileId == worker.Id);
        WorkRecord evidence = await _context.Set<WorkRecord>().Include(item => item.Activities).SingleAsync(item => item.TenantId == tenant.Id && item.FarmId == farm.Id && item.WorkerProfileId == worker.Id);
        evidence.AttendanceId.ShouldBe(attendance.Id); evidence.Status.ShouldBe(WorkRecordStatus.Confirmed);
        evidence.Activities.Count.ShouldBe(1);
        (await _context.Set<WorkerAdvance>().SingleAsync(item => item.TenantId == tenant.Id && item.FarmId == farm.Id &&
            item.WorkerProfileId == worker.Id && item.Reason == label)).WorkerProfileId.ShouldBe(worker.Id);
    }

    [Test]
    public async Task ProfilePersistenceRetainsWorkerAndOperationalLinks()
    {
        await _f.Update(_context, new WorkerProfileInput("EMP-1", "Ms", "First", "Last", "Female", "Address",
            "asset:photo", "Kin", "Sibling", "123", "Kin address", new DateOnly(1990, 1, 2)));
        _context.ChangeTracker.Clear();
        var labour = new LabourRepository(_context);
        WorkerProfile saved = (await labour.GetWorkerAsync(_f.Tenant.Id, _f.Farm.Id, _f.Worker.Id, false, CancellationToken.None))!;
        saved.PersonId.ShouldBe(_f.Person.Id); saved.EmployeeNumber.ShouldBe("EMP-1");
        saved.PhotoReference.ShouldBe("asset:photo"); saved.NextOfKinName.ShouldBe("Kin"); saved.NextOfKinAddress.ShouldBe("Kin address");
        saved.DateOfBirth.ShouldBe(new DateOnly(1990, 1, 2)); saved.EmploymentType.ShouldBe(EmploymentType.Permanent);
        (await labour.GetAttendanceAsync(_f.Tenant.Id, _f.Farm.Id, saved.Id, EmployeeMasterAcceptanceFixture.Date, false, CancellationToken.None))!.Id.ShouldBe(_f.Attendance.Id);
        (await labour.GetWorkRecordAsync(_f.Tenant.Id, _f.Farm.Id, _f.Evidence.Id, false, CancellationToken.None))!.WorkerProfileId.ShouldBe(saved.Id);
        (await _context.Set<WorkerAdvance>().SingleAsync(item => item.TenantId == _f.Tenant.Id && item.FarmId == _f.Farm.Id && item.Id == _f.Advance.Id && item.Reason == _f.Label)).WorkerProfileId.ShouldBe(saved.Id);
    }

    [Test]
    public async Task CoreCreateAndDetailReadKeepMaskedNationalId()
    {
        var labour = new LabourRepository(_context);
        var result = await new CreateWorkerCommandHandler(new FarmSetupRepository(_context), labour, _f.Protector, _f.User, TimeProvider.System)
            .Handle(new CreateWorkerCommand(null, null, null, "Seasonal", EmployeeMasterAcceptanceFixture.Date,
                "SYNTHETIC-SECOND-34", new WorkerProfileInput(FirstName: "New", Surname: "Employee")), CancellationToken.None);
        _context.ChangeTracker.Clear();
        var loaded = await new GetWorkerDetailsQueryHandler(new FarmSetupRepository(_context), labour, _f.User)
            .Handle(new GetWorkerDetailsQuery(result.Worker.Id), CancellationToken.None);
        loaded.Worker.NationalIdMask.ShouldBe("••••••34"); loaded.Worker.DisplayName.ShouldBe("New Employee");
        loaded.Profile!.EmployeeNumber.ShouldBeNull();
        JsonSerializer.Serialize(loaded).ShouldNotContain("SYNTHETICSECOND34");
    }

    [Test]
    public async Task AuthorizedRevealAndCorrectionAreAuditedWithoutPlaintext()
    {
        var labour = new LabourRepository(_context);
        var farms = new FarmSetupRepository(_context);
        var result = await new RevealWorkerNationalIdCommandHandler(farms, labour, _f.Protector, _f.User, TimeProvider.System)
            .Handle(new RevealWorkerNationalIdCommand(_f.Worker.Id, "Synthetic check"), CancellationToken.None);
        result.NationalId.ShouldBe("SYNTHETICCR01312");
        await new CorrectWorkerNationalIdCommandHandler(farms, labour, _f.Protector, _f.User, TimeProvider.System)
            .Handle(new CorrectWorkerNationalIdCommand(_f.Worker.Id, "SYNTHETIC-CORRECTED-34", _f.Worker.Version, "Synthetic correction"), CancellationToken.None);
        var audits = await _context.AuditEvents.Where(item => item.TenantId == _f.Tenant.Id && item.FarmId == _f.Farm.Id &&
            item.SubjectId == _f.Worker.Id && item.CorrelationId == _f.Label).ToListAsync();
        audits.Select(item => item.Action).ShouldContain("NationalIdRevealRequested");
        audits.Select(item => item.Action).ShouldContain("NationalIdRevealSucceeded");
        audits.Select(item => item.Action).ShouldContain("NationalIdCorrected");
        JsonSerializer.Serialize(audits).ShouldNotContain(result.NationalId);
        JsonSerializer.Serialize(audits).ShouldNotContain("SYNTHETIC-CORRECTED-34");
    }

    [Test]
    public async Task DuplicateNationalIdAndEmployeeNumberRemainRejected()
    {
        var labour = new LabourRepository(_context);
        await Should.ThrowAsync<ConflictException>(() => new CreateWorkerCommandHandler(new FarmSetupRepository(_context),
            labour, _f.Protector, _f.User, TimeProvider.System).Handle(new CreateWorkerCommand(null, "Duplicate", null,
                "Casual", EmployeeMasterAcceptanceFixture.Date, "SYNTHETIC-CR013-12"), CancellationToken.None));
        await _f.Update(_context, new WorkerProfileInput(EmployeeNumber: "EMP-1"));
        await Should.ThrowAsync<ConflictException>(() => new CreateWorkerCommandHandler(new FarmSetupRepository(_context),
            labour, _f.Protector, _f.User, TimeProvider.System).Handle(new CreateWorkerCommand(null, "Number duplicate", null,
                "Casual", EmployeeMasterAcceptanceFixture.Date, "SYNTHETIC-NUMBER-34", new WorkerProfileInput(EmployeeNumber: "emp-1")), CancellationToken.None));
    }

    [Test]
    public async Task ForeignTenantCannotReadWorkerAndCanReuseBusinessNumber()
    {
        await _f.Update(_context, new WorkerProfileInput(EmployeeNumber: "EMP-1"));
        EmployeeMasterAcceptanceFixture foreign = new($"AUTOTEST-CR013-FOREIGN-{Guid.NewGuid():N}");
        await foreign.Seed(_context);
        await foreign.Update(_context, new WorkerProfileInput(EmployeeNumber: "EMP-1"));
        var labour = new LabourRepository(_context);
        (await labour.GetWorkerAsync(foreign.Tenant.Id, foreign.Farm.Id, _f.Worker.Id, false, CancellationToken.None)).ShouldBeNull();
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => new GetWorkerDetailsQueryHandler(
            new FarmSetupRepository(_context), labour, foreign.User).Handle(new GetWorkerDetailsQuery(_f.Worker.Id), CancellationToken.None));
    }

    [Test]
    public async Task DatabaseNumberConstraintRejectsRaceAndCompositeForeignTenantLink()
    {
        EmployeeMasterAcceptanceFixture foreign = new($"AUTOTEST-CR013-FOREIGN-{Guid.NewGuid():N}");
        await foreign.Seed(_context);
        await _f.Update(_context, new WorkerProfileInput(EmployeeNumber: "EMP-1"));
        await _transaction.CreateSavepointAsync("constraint_check");
        var exception = await Should.ThrowAsync<PostgresException>(() => _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE labour."WorkerProfiles" SET "TenantId" = {_f.Tenant.Id}, "EmployeeNumber" = NULL
            WHERE "Id" = {foreign.Worker.Id} AND "TenantId" = {foreign.Tenant.Id} AND "FarmId" = {foreign.Farm.Id}
            """));
        exception.SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
        await _transaction.RollbackToSavepointAsync("constraint_check");
        var second = await new CreateWorkerCommandHandler(new FarmSetupRepository(_context), new LabourRepository(_context),
            _f.Protector, _f.User, TimeProvider.System).Handle(new CreateWorkerCommand(null, "Synthetic second", null,
                "Casual", EmployeeMasterAcceptanceFixture.Date, "SYNTHETIC-SECOND-34"), CancellationToken.None);
        await _transaction.CreateSavepointAsync("number_check");
        var duplicate = await Should.ThrowAsync<PostgresException>(() => _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE labour."WorkerProfiles" SET "EmployeeNumber" = 'EMP-1'
            WHERE "Id" = {second.Worker.Id} AND "TenantId" = {_f.Tenant.Id} AND "FarmId" = {_f.Farm.Id}
            """));
        duplicate.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        duplicate.ConstraintName.ShouldBe("UX_WorkerProfiles_Tenant_EmployeeNumber");
        await _transaction.RollbackToSavepointAsync("number_check");
    }

    [Test]
    public async Task DatabaseWorkerConcurrencyRejectsStaleUpdate()
    {
        await _f.Update(_context, new WorkerProfileInput(Address: "First edit"));
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE labour."WorkerProfiles" SET "Version" = "Version" + 1
            WHERE "Id" = {_f.Worker.Id} AND "TenantId" = {_f.Tenant.Id} AND "FarmId" = {_f.Farm.Id}
            """ );
        await Should.ThrowAsync<ConflictException>(() =>
            _f.Update(_context, new WorkerProfileInput(Address: "Stale edit")));
    }
}
