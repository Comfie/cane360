using Cane360.Application.Common.Interfaces;
using Cane360.Application.Labour;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Identity;
using Cane360.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cane360.Infrastructure.IntegrationTests;

internal sealed class EmployeeMasterAcceptanceFixture
{
    public static readonly DateOnly Date = new(2026, 10, 5);
    public static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    public Tenant Tenant { get; }
    public Farm Farm { get; }
    public Person Person { get; }
    public WorkerProfile Worker { get; }
    public Attendance Attendance { get; }
    public WorkRecord Evidence { get; }
    public WorkerAdvance Advance { get; }
    public IUser User { get; }
    public string Label { get; }
    public WorkerSensitiveDataProtector Protector { get; }

    public static ApplicationDbContext Context()
    {
        Environment.GetEnvironmentVariable("CANE360_ACCEPTANCE_TARGET").ShouldBe("RailwayDevelopment");
        IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development")
            .AddEnvironmentVariables().Build();
        return new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Cane360Db")).Options);
    }

    public EmployeeMasterAcceptanceFixture(string label)
    {
        label.ShouldStartWith("AUTOTEST-CR013-");
        Label = label;
        string id = $"cr013-{Guid.NewGuid():N}";
        User = new SyntheticUser(id, label);
        Tenant = Tenant.CreateForGrower(id, label, null);
        Farm = Tenant.CreateFarm("SYNTHETIC", label, "Synthetic", "Synthetic", "Owned", 10m, "Synthetic");
        Person = Farm.AddPerson(label, null, Date);
        Person supervisor = Farm.AddPerson("Synthetic supervisor", null, Date);
        Farm.AssignRole(supervisor, PersonRole.Supervisor, false, Date);
        Field field = Farm.AddField("CR013", label, 5m, null, ReportingAreaSource.Declared, "Synthetic", null);
        CropVariety variety = Tenant.AddCropVariety("CR013", "Synthetic variety");
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), new DateOnly(2027, 2, 1), 100m, Now, id);
        field.ActivateCropCycle(cycle, Now, id);
        ActivityType type = Tenant.AddActivityType("CR013", "Synthetic activity", true, true, ActivityQuantityBasis.Hectares);
        Activity activity = cycle.CreateActivity(Tenant.Id, Farm.Id, field.Id, type, ActivityPlanningKind.Planned, Date, supervisor.Id);
        activity.RecordActualWork(Now, 1m, field.ReportingHectares, null, cycle.StartDate, Now, id, null, 0);
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cane360.slnx"))) directory = directory.Parent;
        string settings = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable."),
            "src", "Web", "appsettings.Development.Local.json");
        var configuration = new ConfigurationBuilder().AddUserSecrets("Cane360-Web-Development").AddEnvironmentVariables().Build();
        var local = new ConfigurationBuilder().AddJsonFile(settings, optional: true).Build();
        Protector = new WorkerSensitiveDataProtector(new ConfigurationBuilder().AddConfiguration(configuration)
            .AddInMemoryCollection(local.GetSection("Cane360Security:NationalId").AsEnumerable()).Build());
        Guid workerId = Guid.NewGuid();
        ProtectedNationalId protectedId = Protector.Protect(Tenant.Id, Farm.Id, workerId, "SYNTHETIC-CR013-12");
        Worker = WorkerProfile.Create(workerId, Tenant.Id, Farm.Id, Person.Id, EmploymentType.Casual, Date,
            protectedId.Ciphertext, protectedId.Nonce, protectedId.Tag, protectedId.KeyId, protectedId.FarmScopedFingerprint, protectedId.DisplayMask);
        Attendance = Attendance.Create(Tenant.Id, Farm.Id, workerId, Date, AttendanceStatus.Present, field.Id, Now, id, null, 0);
        WorkerRate rate = WorkerRate.Create(Tenant.Id, Farm.Id, workerId, PayBasis.Daily, null, 10m, Date, null);
        Evidence = WorkRecord.Create(Tenant.Id, Farm.Id, Attendance.Id, workerId, field.Id, Date, rate, null, [activity.Id], Now, id, null, 0);
        Evidence.RecordSupervisorVerification(supervisor.Id, Now, id, 0);
        Evidence.Confirm(Now, id, 1);
        Rate = rate;
        Period = PayrollPeriod.Create(Tenant.Id, Farm.Id, 2026, 10, Now, id, null);
        Advance = WorkerAdvance.Create(Tenant.Id, Farm.Id, workerId, 10m, label, Date, Period.Id, 1, Now, id, null);
    }

    private WorkerRate Rate { get; }
    private PayrollPeriod Period { get; }

    public async Task Seed(ApplicationDbContext context, bool legacyColumnsOnly = false)
    {
        context.Users.Add(new ApplicationUser {Id = User.Id!, UserName = User.Id + "@example.invalid",
            NormalizedUserName = (User.Id + "@example.invalid").ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N")});
        context.Tenants.Add(Tenant);
        await context.SaveChangesAsync();
        if (legacyColumnsOnly)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO labour."WorkerProfiles" ("Id", "TenantId", "FarmId", "PersonId", "EmploymentType", "ActiveFrom",
                    "Status", "NationalIdCiphertext", "NationalIdNonce", "NationalIdTag", "NationalIdKeyId",
                    "NationalIdFingerprint", "NationalIdMask", "Version", "Created", "LastModified")
                VALUES ({Worker.Id}, {Tenant.Id}, {Farm.Id}, {Person.Id}, 'Casual', {Date}, 'Active',
                    {Worker.NationalIdCiphertext}, {Worker.NationalIdNonce}, {Worker.NationalIdTag}, {Worker.NationalIdKeyId},
                    {Worker.NationalIdFingerprint}, {Worker.NationalIdMask}, 0, {Now}, {Now})
                """);
        }
        else context.WorkerProfiles.Add(Worker);
        context.Set<WorkerRate>().Add(Rate);
        context.Set<Attendance>().Add(Attendance);
        context.Set<WorkRecord>().Add(Evidence);
        context.Set<PayrollPeriod>().Add(Period);
        context.Set<WorkerAdvance>().Add(Advance);
        await context.SaveChangesAsync();
    }

    public Task<WorkerDetailsDto> Update(ApplicationDbContext context, WorkerProfileInput profile,
        long? expectedVersion = null, long? expectedPersonVersion = null) =>
        new UpdateWorkerProfileCommandHandler(new FarmSetupRepository(context), new LabourRepository(context), User, TimeProvider.System)
            .Handle(new UpdateWorkerProfileCommand(Worker.Id, expectedVersion ?? Worker.Version,
                expectedPersonVersion ?? Person.Version, Person.DisplayName, "123", "Permanent", profile), CancellationToken.None);

    private sealed class SyntheticUser(string id, string label) : IUser
    {
        public string? Id => id;
        public List<string>? Roles => [];
        public string? CorrelationId => label;
    }
}
