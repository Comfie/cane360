using System.Text;
using System.Text.Json;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Labour;
using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public sealed class EmployeeMasterTests
{
    [Test]
    public async Task LegacyWorkerRemainsReadableWithSameIdentityAndNullEnrichment()
    {
        Fixture f = new();
        WorkerDetailsDto result = await new GetWorkerDetailsQueryHandler(f.Farms.Object, f.Labour.Object, f.User.Object)
            .Handle(new GetWorkerDetailsQuery(f.Worker.Id), CancellationToken.None);
        result.Worker.Id.ShouldBe(f.Worker.Id);
        result.Worker.PersonId.ShouldBe(f.Person.Id);
        result.Worker.DisplayName.ShouldBe("Legacy name intact");
        result.Profile!.FirstName.ShouldBeNull();
        result.Profile.EmployeeNumber.ShouldBeNull();
        result.Profile.DateOfBirth.ShouldBeNull();
        result.Profile.NextOfKinName.ShouldBeNull();
        f.Worker.IsActiveOn(Fixture.Date).ShouldBeTrue();
    }

    [Test]
    public async Task CompactCreateProtectsNationalIdAndRequiresNoEnrichment()
    {
        Fixture f = new();
        WorkerProfile? added = null;
        f.Labour.Setup(repo => repo.Add(It.IsAny<WorkerProfile>())).Callback<WorkerProfile>(worker => added = worker);
        WorkerDetailsDto result = await new CreateWorkerCommandHandler(f.Farms.Object, f.Labour.Object,
            f.Protector, f.User.Object, f.Clock).Handle(new CreateWorkerCommand(null, null, null, "Seasonal",
                Fixture.Date, "SYNTHETIC-CORE-12", new WorkerProfileInput(FirstName: "First", Surname: "Last")), CancellationToken.None);
        result.Worker.DisplayName.ShouldBe("First Last");
        result.Worker.Phone.ShouldBeNull();
        result.Profile!.EmployeeNumber.ShouldBeNull();
        added!.TenantId.ShouldBe(f.Tenant.Id);
        added.FarmId.ShouldBe(f.Farm.Id);
        added.NationalIdCiphertext.ShouldNotBe(Encoding.UTF8.GetBytes("SYNTHETICCORE12"));
        added.NationalIdMask.ShouldBe("••••••12");
        JsonSerializer.Serialize(result).ShouldNotContain("SYNTHETICCORE12");
        f.Audits.ShouldContain(audit => audit.Action == "WorkerRegistered");
    }

    [Test]
    public async Task ProfileEditUpdatesSharedIdentityWithoutChangingRolesOrEligibility()
    {
        Fixture f = new();
        PersonRoleAssignment role = f.Person.AssignRole(PersonRole.Supervisor, false, Fixture.Date);
        byte[] ciphertext = f.Worker.NationalIdCiphertext.ToArray();
        WorkerDetailsDto result = await f.Update(new WorkerProfileInput(" emp-01 ", "Ms", "First", "Last", "Female",
            " Address ", "asset:photo", "Kin", "Sibling", "123", "Kin address", new DateOnly(1990, 1, 2)), "Permanent");
        result.Worker.DisplayName.ShouldBe("First Last");
        result.Worker.Phone.ShouldBe("456");
        result.Profile!.EmployeeNumber.ShouldBe("EMP-01");
        result.Profile.PhotoReference.ShouldBe("asset:photo");
        result.Profile.NextOfKinName.ShouldBe("Kin");
        result.Profile.NextOfKinAddress.ShouldBe("Kin address");
        result.Profile.Address.ShouldBe("Address");
        result.Profile.DateOfBirth.ShouldBe(new DateOnly(1990, 1, 2));
        result.Worker.EmploymentType.ShouldBe("Permanent");
        f.Person.RoleAssignments.Single().ShouldBeSameAs(role);
        role.EffectiveTo.ShouldBeNull();
        f.Worker.ActiveFrom.ShouldBe(Fixture.Date);
        f.Worker.NationalIdCiphertext.ShouldBe(ciphertext);
        f.Worker.IsActiveOn(Fixture.Date).ShouldBeTrue();
        f.Audits.ShouldContain(audit => audit.Action == "WorkerProfileUpdated");
    }

    [Test]
    public async Task NullStructuredNamesDoNotSplitOrDestroyLegacyDisplayName()
    {
        Fixture f = new();
        WorkerDetailsDto result = await f.Update(new WorkerProfileInput());
        result.Worker.DisplayName.ShouldBe("Legacy name intact");
        result.Profile!.FirstName.ShouldBeNull();
        result.Profile.Surname.ShouldBeNull();
        result.Profile.DateOfBirth.ShouldBeNull();
    }

    [Test]
    public async Task EnrichmentCanBeClearedAndKinUpdated()
    {
        Fixture f = new();
        await f.Update(new WorkerProfileInput(PhotoReference: "asset:photo", NextOfKinName: "Old kin"));
        var result = await f.Update(new WorkerProfileInput(NextOfKinName: "New kin", NextOfKinPhone: "789"));
        result.Profile!.PhotoReference.ShouldBeNull();
        result.Profile.NextOfKinName.ShouldBe("New kin");
        result.Profile.NextOfKinPhone.ShouldBe("789");
    }

    [TestCase(-1)]
    [TestCase(0)]
    public async Task BirthDateAllowsPastAndToday(int offset)
    {
        Fixture f = new();
        var result = await f.Update(new WorkerProfileInput(DateOfBirth: Fixture.Date.AddDays(offset)));
        result.Profile!.DateOfBirth.ShouldBe(Fixture.Date.AddDays(offset));
    }

    [Test]
    public async Task FutureBirthDateIsRejectedByValidatorAndDomain()
    {
        Fixture f = new();
        WorkerProfileInput input = new(DateOfBirth: Fixture.Date.AddDays(1));
        new WorkerProfileInputValidator(f.Clock).Validate(input).IsValid.ShouldBeFalse();
        await Should.ThrowAsync<ValidationException>(() => f.Update(input));
        f.Worker.DateOfBirth.ShouldBeNull();
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void PartialStructuredNameAndOversizedPhotoReferenceAreRejected()
    {
        Fixture f = new();
        var validator = new WorkerProfileInputValidator(f.Clock);
        validator.Validate(new WorkerProfileInput(FirstName: "Only first")).IsValid.ShouldBeFalse();
        validator.Validate(new WorkerProfileInput(PhotoReference: new string('x', 241))).IsValid.ShouldBeFalse();
        validator.Validate(new WorkerProfileInput(Sex: "Unrecognised")).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ExistingCreateValidationAndLegacyDisplayNameContractRemain()
    {
        Fixture f = new();
        var validator = new CreateWorkerCommandValidator(f.Clock);
        validator.Validate(new CreateWorkerCommand(null, "Legacy", null, "Casual", Fixture.Date, "SYNTHETIC-12")).IsValid.ShouldBeTrue();
        validator.Validate(new CreateWorkerCommand(null, null, null, "Casual", Fixture.Date, "SYNTHETIC-12",
            new WorkerProfileInput(FirstName: "First", Surname: "Last"))).IsValid.ShouldBeTrue();
        validator.Validate(new CreateWorkerCommand(null, null, null, "Unknown", default, "")).IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task DuplicateEmployeeNumberIsRejectedWithinTenant()
    {
        Fixture f = new();
        f.Labour.Setup(repo => repo.HasEmployeeNumberAsync(f.Tenant.Id, "EMP-1", f.Worker.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Should.ThrowAsync<ConflictException>(() => f.Update(new WorkerProfileInput(EmployeeNumber: " emp-1 ")));
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StaleWorkerVersionCannotSaveProfile()
    {
        Fixture f = new();
        await f.Update(new WorkerProfileInput());
        await Should.ThrowAsync<ValidationException>(() => f.Handler.Handle(new UpdateWorkerProfileCommand(f.Worker.Id,
            0, f.Person.Version, f.Person.DisplayName, null, "Casual", new WorkerProfileInput()), CancellationToken.None));
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StaleSharedPersonVersionCannotSaveProfile()
    {
        Fixture f = new();
        f.Person.UpdateIdentity("Other edit", null, f.Person.Version);
        await Should.ThrowAsync<ValidationException>(() => f.Handler.Handle(new UpdateWorkerProfileCommand(f.Worker.Id,
            0, 0, "Name", null, "Casual", new WorkerProfileInput()), CancellationToken.None));
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ForeignWorkerIsNotFoundUsingTenantAndFarmScope()
    {
        Fixture f = new();
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => f.Handler.Handle(
            new UpdateWorkerProfileCommand(Guid.NewGuid(), 0, 0, "Name", null, "Casual", new WorkerProfileInput()), CancellationToken.None));
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SupervisorWithoutExistingManagerMembershipCannotEdit()
    {
        Fixture f = new();
        f.Farms.Setup(repo => repo.GetTenantForUserAsync("user", true, It.IsAny<CancellationToken>())).ReturnsAsync((Tenant?)null);
        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => f.Update(new WorkerProfileInput()));
    }

    [Test]
    public async Task ArchivedEmployeeEnrichmentDoesNotReactivateEmployment()
    {
        Fixture f = new();
        f.Worker.Archive(Fixture.Date, 0);
        await f.Update(new WorkerProfileInput(Address: "Updated address"));
        f.Worker.Status.ShouldBe(RecordStatus.Archived);
        f.Worker.ActiveTo.ShouldBe(Fixture.Date);
        f.Worker.IsActiveOn(Fixture.Date).ShouldBeFalse();
    }

    [Test]
    public async Task CorrectionProtectsIdAndAuditsWithoutSecretOrFreeTextReason()
    {
        Fixture f = new();
        var result = await f.Correct("SYNTHETIC-NEW-34");
        result.Worker.NationalIdMask.ShouldBe("••••••34");
        f.Worker.NationalIdCiphertext.ShouldNotBe(Encoding.UTF8.GetBytes("SYNTHETICNEW34"));
        f.Audits.ShouldContain(audit => audit.Action == "NationalIdCorrected");
        JsonSerializer.Serialize(f.Audits).ShouldNotContain("SYNTHETIC-NEW-34");
        JsonSerializer.Serialize(result).ShouldNotContain("SYNTHETICNEW34");
    }

    [Test]
    public async Task DuplicateCorrectionIsRejectedBeforeSave()
    {
        Fixture f = new();
        f.Labour.Setup(repo => repo.HasNationalIdFingerprintAsync(f.Tenant.Id, f.Farm.Id,
            It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Should.ThrowAsync<ConflictException>(() => f.Correct("SYNTHETIC-OTHER-34"));
        f.Labour.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SameNationalIdCorrectionDoesNotTreatSelfAsDuplicate()
    {
        Fixture f = new();
        await f.Correct("SYNTHETIC-OLD-12");
        f.Labour.Verify(repo => repo.HasNationalIdFingerprintAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RevealRetainsMaskAndRecordsRequestedAndSucceededAudits()
    {
        Fixture f = new();
        var result = await new RevealWorkerNationalIdCommandHandler(f.Farms.Object, f.Labour.Object,
            f.Protector, f.User.Object, f.Clock).Handle(new RevealWorkerNationalIdCommand(f.Worker.Id, "Synthetic check"), CancellationToken.None);
        result.NationalId.ShouldBe("SYNTHETICOLD12");
        f.Worker.NationalIdMask.ShouldBe("••••••12");
        f.Audits.Select(a => a.Action).ShouldBe(new[] {"NationalIdRevealRequested", "NationalIdRevealSucceeded"});
        JsonSerializer.Serialize(f.Audits).ShouldNotContain(result.NationalId);
    }

    [Test]
    public async Task ManagerCanEditButCannotRevealOrCorrectNationalId()
    {
        Fixture f = new();
        f.Tenant.AddMembership("manager", f.Person.Id, TenantSecurityRoles.FarmManager);
        f.User.Setup(u => u.Id).Returns("manager");
        f.Farms.Setup(repo => repo.GetTenantForUserAsync("manager", true, It.IsAny<CancellationToken>())).ReturnsAsync(f.Tenant);
        f.Farms.Setup(repo => repo.GetTenantForUserAsync("manager", false, It.IsAny<CancellationToken>())).ReturnsAsync(f.Tenant);
        await f.Update(new WorkerProfileInput(Address: "Manager edit"));
        await Should.ThrowAsync<ForbiddenAccessException>(() => f.Correct("SYNTHETIC-NEW-34"));
        await Should.ThrowAsync<ForbiddenAccessException>(() => new RevealWorkerNationalIdCommandHandler(f.Farms.Object,
            f.Labour.Object, f.Protector, f.User.Object, f.Clock).Handle(new RevealWorkerNationalIdCommand(f.Worker.Id, "check"), CancellationToken.None));
    }

    [Test]
    public async Task IdentityEnrichmentKeepsAttendanceEvidenceAndPayrollSnapshotReferences()
    {
        Fixture f = new();
        Guid fieldId = Guid.NewGuid();
        Guid activityId = Guid.NewGuid();
        DateTimeOffset now = f.Clock.GetUtcNow();
        var attendance = Attendance.Create(f.Tenant.Id, f.Farm.Id, f.Worker.Id, Fixture.Date,
            AttendanceStatus.Present, fieldId, now, "user", null, 0);
        var rate = WorkerRate.Create(f.Tenant.Id, f.Farm.Id, f.Worker.Id, PayBasis.Daily, null, 10m, Fixture.Date, null);
        var evidence = WorkRecord.Create(f.Tenant.Id, f.Farm.Id, attendance.Id, f.Worker.Id, fieldId,
            Fixture.Date, rate, null, [activityId], now, "user", null, 0);
        Guid calculationId = Guid.NewGuid();
        Guid lineId = Guid.NewGuid();
        var earning = PayrollEarningLine.Create(lineId, calculationId, f.Tenant.Id, f.Farm.Id, f.Worker.Id,
            evidence.Id, "Daily", Fixture.Date, attendance.Id, attendance.Version, now, now, fieldId,
            "Synthetic activity", 1m, "day", "Daily", 10m, rate.Id, rate.Version, "Synthetic fingerprint");
        var payroll = PayrollWorkerLine.Create(lineId, calculationId, f.Tenant.Id, f.Farm.Id, f.Worker.Id,
            f.Person.DisplayName, [earning], []);
        await f.Update(new WorkerProfileInput(FirstName: "New", Surname: "Name"));
        attendance.WorkerProfileId.ShouldBe(f.Worker.Id);
        evidence.WorkerProfileId.ShouldBe(f.Worker.Id);
        evidence.AttendanceId.ShouldBe(attendance.Id);
        evidence.Activities.Single().ActivityId.ShouldBe(activityId);
        payroll.WorkerProfileId.ShouldBe(f.Worker.Id);
        payroll.WorkerNameSnapshot.ShouldBe("Legacy name intact");
        payroll.GrossAmountUsd.ShouldBe(10m);
        earning.EvidenceId.ShouldBe(evidence.Id);
        earning.AttendanceId.ShouldBe(attendance.Id);
    }

    private sealed class Fixture
    {
        public static readonly DateOnly Date = new(2026, 10, 5);
        public Tenant Tenant { get; } = Tenant.CreateForGrower("user", "Synthetic owner", null);
        public Farm Farm { get; }
        public Person Person { get; }
        public WorkerProfile Worker { get; }
        public Mock<IFarmSetupRepository> Farms { get; } = new();
        public Mock<ILabourRepository> Labour { get; } = new();
        public Mock<IUser> User { get; } = new();
        public List<AuditEvent> Audits { get; } = [];
        public IWorkerSensitiveDataProtector Protector { get; }
        public TimeProvider Clock { get; } = new FixedClock();
        public UpdateWorkerProfileCommandHandler Handler => new(Farms.Object, Labour.Object, User.Object, Clock);
        public Fixture()
        {
            Farm = Tenant.CreateFarm("SYNTHETIC", "Synthetic farm", "Synthetic", "Synthetic", "Owned", 10m, "Synthetic");
            Person = Farm.AddPerson("Legacy name intact", null, Date);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cane360Security:NationalId:ActiveKeyId"] = "unit",
                ["Cane360Security:NationalId:Keys:unit"] = Convert.ToBase64String(new byte[32]),
                ["Cane360Security:NationalId:FingerprintKey"] = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray())}).Build();
            Protector = new WorkerSensitiveDataProtector(config);
            Guid id = Guid.NewGuid();
            ProtectedNationalId protectedId = Protector.Protect(Tenant.Id, Farm.Id, id, "SYNTHETIC-OLD-12");
            Worker = WorkerProfile.Create(id, Tenant.Id, Farm.Id, Person.Id, EmploymentType.Casual, Date,
                protectedId.Ciphertext, protectedId.Nonce, protectedId.Tag, protectedId.KeyId,
                protectedId.FarmScopedFingerprint, protectedId.DisplayMask);
            User.Setup(u => u.Id).Returns("user"); User.Setup(u => u.CorrelationId).Returns("synthetic-unit");
            foreach (bool track in new[] {true, false})
            {
                Farms.Setup(repo => repo.GetTenantForUserAsync("user", track, It.IsAny<CancellationToken>())).ReturnsAsync(Tenant);
                Labour.Setup(repo => repo.GetWorkerAsync(Tenant.Id, Farm.Id, Worker.Id, track, It.IsAny<CancellationToken>())).ReturnsAsync(Worker);
            }
            Labour.Setup(repo => repo.GetRatesAsync(Tenant.Id, Farm.Id, Worker.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            Labour.Setup(repo => repo.Add(It.IsAny<AuditEvent>())).Callback<AuditEvent>(Audits.Add);
        }
        public Task<WorkerDetailsDto> Update(WorkerProfileInput profile, string type = "Casual") => Handler.Handle(
            new UpdateWorkerProfileCommand(Worker.Id, Worker.Version, Person.Version, Person.DisplayName, "456", type, profile), CancellationToken.None);
        public Task<WorkerDetailsDto> Correct(string id) => new CorrectWorkerNationalIdCommandHandler(Farms.Object,
            Labour.Object, Protector, User.Object, Clock).Handle(new CorrectWorkerNationalIdCommand(Worker.Id, id,
                Worker.Version, id), CancellationToken.None);
    }
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    }
}
