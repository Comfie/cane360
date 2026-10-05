using System.Security.Cryptography;
using Ardalis.GuardClauses;
using System.Text.Json;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.FarmSetup;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.FarmSetup;

public sealed class FarmOwnerProfileTests
{
    [Test]
    public async Task NewOwnerCanBeCreatedWithStructuredProfileAndProtectedId()
    {
        Fixture fixture = new();
        var repository = new Mock<IFarmSetupRepository>();
        var result = await new CreateGrowerFarmCommandHandler(repository.Object, fixture.User.Object,
            fixture.Protector, TimeProvider.System).Handle(new CreateGrowerFarmCommand("New owner", "123",
            "TEST", "Synthetic", "Synthetic", "Synthetic", "Owned", 10, "Synthetic",
            new FarmOwnerProfileInput(FirstName: "Synthetic", Association: "Association",
                NationalId: "63-123456-A-12")), CancellationToken.None);
        result.Grower!.FirstName.ShouldBe("Synthetic");
        result.Grower.Association.ShouldBe("Association");
        result.Grower.NationalIdMask.ShouldBe("••••••12");
        repository.Verify(repo => repo.Add(It.Is<AuditEvent>(item => item.Action == "ProfileCreated")), Times.Once);
        repository.Verify(repo => repo.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task ExistingRecordRemainsReadableWithoutEnhancedFields()
    {
        Fixture fixture = new();
        FarmSetupDto result = await new GetFarmSetupQueryHandler(fixture.Repository.Object, fixture.User.Object)
            .Handle(new GetFarmSetupQuery(), CancellationToken.None);
        result.Grower!.DisplayName.ShouldBe("Existing owner");
        result.Grower.FirstName.ShouldBeNull();
        result.Grower.Active.ShouldBeTrue();
        result.Farm!.FarmModelId.ShouldBeNull();
    }

    [Test]
    public async Task NewFieldsSaveUpdateAndLegacyUpdatePreservesThem()
    {
        Fixture fixture = new();
        var input = new FarmOwnerProfileInput("Mrs", "Tariro", "Moyo", "Female", "G-1", "Association",
            "M-1", "Owner address", "owner@example.invalid", "asset:synthetic-photo", false, "63-123456-A-12");
        FarmSetupDto result = await fixture.Update(input);
        result.Grower!.FirstName.ShouldBe("Tariro");
        result.Grower.GrowerNumber.ShouldBe("G-1");
        result.Grower.Active.ShouldBeFalse();
        result.Grower.PhotoReference.ShouldBe("asset:synthetic-photo");
        result.Grower.NationalIdMask.ShouldBe("••••••12");
        var updated = await fixture.Update(input with { FirstName = "Updated", Active = true, NationalId = null });
        updated.Grower!.FirstName.ShouldBe("Updated");
        updated.Grower.Active.ShouldBeTrue();
        (await fixture.Update(null)).Grower!.NationalIdMask.ShouldBe("••••••12");
        fixture.Tenant.GrowerProfile.NationalIdCiphertext.ShouldNotBe(System.Text.Encoding.UTF8.GetBytes("63123456A12"));
        JsonSerializer.Serialize(updated).ShouldNotContain("Ciphertext");
        JsonSerializer.Serialize(updated).ShouldNotContain("63123456A12");
        fixture.Audit.ShouldContain(item => item.Action == "NationalIdChanged");
        fixture.Audit.ShouldContain(item => item.Action == "ProfileUpdated");
        fixture.Audit.ShouldAllBe(item => !item.SafeSummary.Contains("Tariro") && !item.SafeSummary.Contains("123456"));
    }

    [Test]
    public async Task NormalizedIdCorrectionPreservesFingerprintAndReadMask()
    {
        Fixture fixture = new();
        await fixture.Update(new FarmOwnerProfileInput(NationalId: "63-123456-a-12"));
        byte[] before = fixture.Tenant.GrowerProfile.NationalIdFingerprint!;
        await fixture.Update(new FarmOwnerProfileInput(NationalId: "63 123456 A 12"));
        fixture.Tenant.GrowerProfile.NationalIdFingerprint.ShouldBe(before);
        fixture.Tenant.GrowerProfile.NationalIdMask.ShouldBe("••••••12");
    }

    [Test]
    public async Task RevealIsGrowerOnlyAndAuditedBeforeAndAfterDecryption()
    {
        Fixture fixture = new();
        await fixture.Update(new FarmOwnerProfileInput(NationalId: "63-123456-A-12"));
        var result = await new RevealFarmOwnerNationalIdCommandHandler(fixture.Repository.Object,
            fixture.User.Object, fixture.Protector, TimeProvider.System)
            .Handle(new RevealFarmOwnerNationalIdCommand(), CancellationToken.None);
        result.NationalId.ShouldBe("63123456A12");
        fixture.Audit.TakeLast(2).Select(item => item.Action).ShouldBe(
            new[] { "NationalIdRevealRequested", "NationalIdRevealSucceeded" });
    }

    [Test]
    public async Task ManagerCanEditButCannotRevealNationalId()
    {
        Fixture fixture = new();
        var manager = fixture.Tenant.ActiveFarm!.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        fixture.Tenant.AddMembership("manager", manager.Id, TenantSecurityRoles.FarmManager);
        fixture.User.SetupGet(user => user.Id).Returns("manager");
        fixture.Repository.Setup(repo => repo.GetTenantForUserAsync("manager", true, CancellationToken.None)).ReturnsAsync(fixture.Tenant);
        fixture.Repository.Setup(repo => repo.GetTenantForUserAsync("manager", false, CancellationToken.None)).ReturnsAsync(fixture.Tenant);
        await fixture.Update(new FarmOwnerProfileInput(GrowerNumber: "G-2", NationalId: "63-123456-A-12"));
        fixture.Tenant.GrowerProfile.GrowerNumber.ShouldBe("G-2");
        await Should.ThrowAsync<ForbiddenAccessException>(() => new RevealFarmOwnerNationalIdCommandHandler(
            fixture.Repository.Object, fixture.User.Object, fixture.Protector, TimeProvider.System)
            .Handle(new RevealFarmOwnerNationalIdCommand(), CancellationToken.None));
        fixture.Audit.ShouldNotContain(item => item.Action.StartsWith("NationalIdReveal"));
    }

    [Test]
    public async Task UnresolvedTenantCannotEditOrReveal()
    {
        Fixture fixture = new();
        fixture.User.SetupGet(user => user.Id).Returns("foreign-user");
        await Should.ThrowAsync<ValidationException>(() => fixture.Update(new FarmOwnerProfileInput(FirstName: "Foreign")));
        await Should.ThrowAsync<ForbiddenAccessException>(() => new RevealFarmOwnerNationalIdCommandHandler(
            fixture.Repository.Object, fixture.User.Object, fixture.Protector, TimeProvider.System)
            .Handle(new RevealFarmOwnerNationalIdCommand(), CancellationToken.None));
        fixture.Audit.ShouldBeEmpty();
    }

    [Test]
    public async Task ProtectedIdCannotDecryptInAnotherTenantOrProfile()
    {
        Fixture fixture = new();
        await fixture.Update(new FarmOwnerProfileInput(NationalId: "63-123456-A-12"));
        var profile = fixture.Tenant.GrowerProfile;
        Should.Throw<AuthenticationTagMismatchException>(() => fixture.Protector.Reveal(Guid.NewGuid(),
            fixture.Tenant.ActiveFarm!.Id, profile.Id, profile.NationalIdCiphertext!, profile.NationalIdNonce!,
            profile.NationalIdTag!, profile.NationalIdKeyId!));
    }

    [TestCase("invalid", "Female", "12345", false)]
    [TestCase("owner@example.invalid", "Unexpected", "12345", false)]
    [TestCase("owner@example.invalid", "Female", "1", false)]
    [TestCase(null, null, null, true)]
    public void OptionalProfileValidationRejectsInvalidSuppliedValues(string? email, string? sex, string? nationalId, bool valid)
    {
        new FarmOwnerProfileInputValidator().Validate(new FarmOwnerProfileInput(Email: email, Sex: sex,
            NationalId: nationalId)).IsValid.ShouldBe(valid);
    }

    [Test]
    public void ExistingRequiredFieldsRemainRequired()
    {
        new UpdateFarmInformationCommandValidator().Validate(new UpdateFarmInformationCommand("", null, "", "",
            "", "", "", 0, "")).IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task ModelCanBeAssignedChangedAndCleared()
    {
        Fixture fixture = new();
        var first = FarmModel.Create(fixture.Tenant.Id, "FIRST", "First");
        var second = FarmModel.Create(fixture.Tenant.Id, "SECOND", "Second");
        fixture.Models.AddRange([first, second]);
        (await fixture.Update(null, first.Id, true)).Farm!.FarmModelId.ShouldBe(first.Id);
        (await fixture.Update(null, second.Id, true)).Farm!.FarmModelId.ShouldBe(second.Id);
        (await fixture.Update(null, null, true)).Farm!.FarmModelId.ShouldBeNull();
        fixture.Audit.Count(item => item.Action == "FarmModelAssigned").ShouldBe(3);
    }

    [Test]
    public async Task MissingModelIsValidationFailure()
    {
        Fixture fixture = new();
        await Should.ThrowAsync<ValidationException>(() => fixture.Update(null, Guid.NewGuid(), true));
        fixture.Repository.Verify(repo => repo.SaveChangesAsync(CancellationToken.None), Times.Never);
    }

    [Test]
    public void InactiveModelCanRemainButCannotBeNewlyAssigned()
    {
        Fixture fixture = new();
        FarmModel model = FarmModel.Create(fixture.Tenant.Id, "MODEL", "Model");
        fixture.Tenant.ActiveFarm!.AssignModel(model);
        model.Update("Model", false, 1);
        fixture.Tenant.ActiveFarm.AssignModel(model);
        fixture.Tenant.ActiveFarm.AssignModel(null);
        Should.Throw<InvalidOperationException>(() => fixture.Tenant.ActiveFarm.AssignModel(model));
    }

    [Test]
    public void DomainRejectsCrossTenantModelEvenIfRepositoryReturnsIt()
    {
        Fixture fixture = new();
        Should.Throw<InvalidOperationException>(() => fixture.Tenant.ActiveFarm!.AssignModel(
            FarmModel.Create(Guid.NewGuid(), "FOREIGN", "Foreign")));
    }

    [Test]
    public async Task ModelAdministrationEnforcesUniqueCodesAndVersion()
    {
        Fixture fixture = new();
        var handler = new SaveFarmModelCommandHandler(fixture.Repository.Object, fixture.User.Object, TimeProvider.System);
        var model = await handler.Handle(new SaveFarmModelCommand("model", "Model"), CancellationToken.None);
        model.Code.ShouldBe("MODEL");
        await Should.ThrowAsync<ValidationException>(() => handler.Handle(new SaveFarmModelCommand("MODEL", "Duplicate"), CancellationToken.None));
        var updated = await handler.Handle(new SaveFarmModelCommand("MODEL", "Renamed", false, model.Id, 1), CancellationToken.None);
        updated.Active.ShouldBeFalse();
        updated.Version.ShouldBe(2);
        await Should.ThrowAsync<ConflictException>(() => handler.Handle(new SaveFarmModelCommand("MODEL", "Stale", true, model.Id, 1), CancellationToken.None));
    }

    [Test]
    public async Task ModelAdministrationCannotUpdateAnotherTenantModel()
    {
        Fixture fixture = new();
        await Should.ThrowAsync<NotFoundException>(() => new SaveFarmModelCommandHandler(fixture.Repository.Object,
            fixture.User.Object, TimeProvider.System).Handle(new SaveFarmModelCommand("FOREIGN", "Foreign", true,
            Guid.NewGuid(), 1), CancellationToken.None));
    }

    private sealed class Fixture
    {
        public Tenant Tenant { get; } = Tenant.CreateForGrower("owner", "Existing owner", "123");
        public Mock<IFarmSetupRepository> Repository { get; } = new();
        public Mock<IUser> User { get; } = new();
        public List<FarmModel> Models { get; } = [];
        public List<AuditEvent> Audit { get; } = [];
        public WorkerSensitiveDataProtector Protector { get; } = new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cane360Security:NationalId:ActiveKeyId"] = "synthetic-test",
                ["Cane360Security:NationalId:Keys:synthetic-test"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["Cane360Security:NationalId:FingerprintKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            }).Build());
        public Fixture()
        {
            Tenant.CreateFarm("TEST", "Synthetic farm", "Synthetic", "Synthetic", "Owned", 10, "Synthetic");
            User.SetupGet(user => user.Id).Returns("owner");
            Repository.Setup(repo => repo.GetTenantWorkspaceForUserAsync("owner", CancellationToken.None)).ReturnsAsync(Tenant);
            Repository.Setup(repo => repo.GetTenantForUserAsync("owner", true, CancellationToken.None)).ReturnsAsync(Tenant);
            Repository.Setup(repo => repo.GetTenantForUserAsync("owner", false, CancellationToken.None)).ReturnsAsync(Tenant);
            Repository.Setup(repo => repo.GetFarmModelsAsync(Tenant.Id, It.IsAny<bool>(), CancellationToken.None)).ReturnsAsync(Models);
            Repository.Setup(repo => repo.Add(It.IsAny<FarmModel>())).Callback<FarmModel>(Models.Add);
            Repository.Setup(repo => repo.Add(It.IsAny<AuditEvent>())).Callback<AuditEvent>(Audit.Add);
        }
        public Task<FarmSetupDto> Update(FarmOwnerProfileInput? input, Guid? modelId = null, bool changeModel = false)
            => new UpdateFarmInformationCommandHandler(Repository.Object, User.Object, Protector, TimeProvider.System)
                .Handle(new UpdateFarmInformationCommand("Existing owner", "123", "TEST", "Synthetic farm",
                    "Synthetic", "Synthetic", "Owned", 10, "Synthetic", input, modelId, changeModel), CancellationToken.None);
    }
}
