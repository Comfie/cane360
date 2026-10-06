using Ardalis.GuardClauses;
using Cane360.Application.Common.Behaviours;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class InventoryCategoryTests
{
    [Test]
    public void CategoryNormalizesCodeNameAndOptionalDescription()
    {
        InventoryCategory category = InventoryCategory.Create(Guid.NewGuid(), " fuel ", " Fuel ", " Diesel ", 5);
        category.Code.ShouldBe("FUEL");
        category.Name.ShouldBe("Fuel");
        category.NormalizedName.ShouldBe("FUEL");
        category.Description.ShouldBe("Diesel");
        category.DisplayOrder.ShouldBe(5);
        category.Active.ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void WhitespaceNameRejected(string name)
    {
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.NewGuid(), "CODE", name));
        new CreateInventoryCategoryCommandValidator().Validate(new CreateInventoryCategoryCommand("CODE", name, null, 0)).IsValid.ShouldBeFalse();
    }

    [TestCase(-1)]
    [TestCase(10001)]
    public void DisplayOrderOutsideBoundsRejected(int order)
    {
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.NewGuid(), "CODE", "Name", null, order));
    }

    [Test]
    public void NameDescriptionAndCodeLimitsAreEnforced()
    {
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.NewGuid(), "CODE", new string('x', 121)));
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.NewGuid(), "CODE", "Name", new string('x', 501)));
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.NewGuid(), new string('x', 41), "Name"));
        Should.Throw<ArgumentException>(() => InventoryCategory.Create(Guid.Empty, "CODE", "Name"));
    }

    [Test]
    public void RenamePreservesIdentityTenantAndImmutableCode()
    {
        InventoryCategory category = InventoryCategory.Create(Guid.NewGuid(), "FERT", "Fertiliser");
        Guid id = category.Id;
        Guid tenantId = category.TenantId;
        category.Update("Fertiliser & Soil Amendments", "Description", 7, category.Version);
        category.Id.ShouldBe(id);
        category.TenantId.ShouldBe(tenantId);
        category.Code.ShouldBe("FERT");
        category.Name.ShouldBe("Fertiliser & Soil Amendments");
        category.Description.ShouldBe("Description");
        category.DisplayOrder.ShouldBe(7);
        category.Version.ShouldBe(2);
    }

    [Test]
    public void ActivateDeactivateAndRepeatedStateUseVersionRules()
    {
        InventoryCategory category = InventoryCategory.Create(Guid.NewGuid(), "CODE", "Name");
        category.SetActive(false, 1);
        category.Active.ShouldBeFalse();
        category.SetActive(false, 2);
        category.Version.ShouldBe(2);
        category.SetActive(true, 2);
        category.Active.ShouldBeTrue();
        category.Version.ShouldBe(3);
        Should.Throw<InvalidOperationException>(() => category.Update("Stale", null, 0, 1));
        Should.Throw<InvalidOperationException>(() => category.SetActive(false, 1));
    }

    [Test]
    public void NewTenantsRetainExactlyFourLegacyOptionsAndCodes()
    {
        Tenant tenant = Tenant.CreateForGrower("owner", "Synthetic tenant", null);
        tenant.InventoryCategories.Select(category => category.Code).ShouldBe(
            Enum.GetNames<InventoryItemCategory>(), ignoreOrder: true);
        tenant.InventoryCategories.ShouldAllBe(category => category.TenantId == tenant.Id && category.Active);
        tenant.InventoryCategories.Single(category => category.Code == "SeedAndPlantingMaterial")
            .Name.ShouldBe("Seed And Planting Material");
    }

    [Test]
    public void InactiveCategoryStaysOnExistingItemButCannotBeNewlyAssigned()
    {
        Guid tenantId = Guid.NewGuid();
        InventoryCategory category = InventoryCategory.Create(tenantId, "FERT", "Fertiliser");
        UnitOfMeasure unit = UnitOfMeasure.Create(tenantId, "KG", "Kilogram", "Mass", 6);
        InventoryItem item = InventoryItem.Create(tenantId, Guid.NewGuid(), "ITEM", "Item", category,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        category.SetActive(false, 1);
        item.Category.ShouldBe("FERT");
        item.Status.ShouldBe(InventoryRecordStatus.Active);
        Should.Throw<InvalidOperationException>(() => InventoryItem.Create(tenantId, Guid.NewGuid(), "NEW", "Item", category,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None));
    }

    [Test]
    public void CrossTenantCategoryAssignmentRejected()
    {
        Guid tenantId = Guid.NewGuid();
        UnitOfMeasure unit = UnitOfMeasure.Create(tenantId, "KG", "Kilogram", "Mass", 6);
        InventoryCategory category = InventoryCategory.Create(Guid.NewGuid(), "FERT", "Fertiliser");
        Should.Throw<InvalidOperationException>(() => InventoryItem.Create(tenantId, Guid.NewGuid(), "ITEM", "Item", category,
            unit, null, LotTrackingPolicy.None, ExpiryPolicy.None));
    }

    [Test]
    public async Task ManagerCreatesTenantCategoryAndAuditWithoutLedgerWrites()
    {
        Fixture fixture = new();
        InventoryCategoryDto result = await fixture.Create.Handle(new("FUEL", "Fuel", "Description", 6), default);
        result.Code.ShouldBe("FUEL");
        fixture.Inventory.Verify(repository => repository.Add(It.Is<InventoryCategory>(category =>
            category.Id == result.Id && category.TenantId == fixture.Tenant.Id)), Times.Once);
        fixture.Inventory.Verify(repository => repository.Add(It.Is<AuditEvent>(audit =>
            audit.SubjectId == result.Id && audit.Action == "Created" && audit.TenantId == fixture.Tenant.Id)), Times.Once);
        fixture.Inventory.Verify(repository => repository.GetCategoriesAsync(fixture.Tenant.Id, false, default), Times.Once);
        fixture.Inventory.Verify(repository => repository.SaveChangesAsync(default), Times.Once);
        fixture.Inventory.VerifyNoOtherCalls();
    }

    [TestCase(" fertiliser ")]
    [TestCase("FERTILISER")]
    public async Task DuplicateNormalizedNameRejected(string name)
    {
        Fixture fixture = new();
        await Should.ThrowAsync<ValidationException>(() => fixture.Create.Handle(new("DIFFERENT", name, null, 0), default));
        fixture.Inventory.Verify(repository => repository.SaveChangesAsync(default), Times.Never);
    }

    [Test]
    public async Task LegacyCodeCannotBeDuplicatedWithDifferentCase()
    {
        Fixture fixture = new();
        await Should.ThrowAsync<ValidationException>(() => fixture.Create.Handle(new("FERTILISER", "Different", null, 0), default));
    }

    [TestCase(TenantSecurityRoles.Grower)]
    [TestCase(TenantSecurityRoles.Supervisor)]
    public async Task NonManagerRejectedByAuthorizationPipeline(string role)
    {
        Mock<IUser> user = new(); user.Setup(value => value.Id).Returns("user");
        Mock<IFarmSetupRepository> farms = new();
        farms.Setup(value => value.GetActiveTenantSecurityRoleForUserAsync("user", default)).ReturnsAsync(role);
        AuthorizationBehaviour<CreateInventoryCategoryCommand, InventoryCategoryDto> behaviour =
            new(user.Object, Mock.Of<IIdentityService>(), farms.Object);
        await Should.ThrowAsync<ForbiddenAccessException>(() => behaviour.Handle(new("FUEL", "Fuel", null, 0),
            _ => throw new InvalidOperationException("Must not dispatch"), default));
    }

    [Test]
    public async Task OwnerRejectedByHandlerDefenceInDepth()
    {
        Fixture fixture = new(TenantSecurityRoles.Grower);
        await Should.ThrowAsync<ForbiddenAccessException>(() => fixture.Create.Handle(new("FUEL", "Fuel", null, 0), default));
    }

    [Test]
    public async Task UpdatePreservesIdentityAndAuditsOnlyCategory()
    {
        Fixture fixture = new();
        InventoryCategory category = fixture.Category;
        InventoryCategoryDto result = await fixture.Update.Handle(new(category.Id, "Renamed", "Changed", 8, 1), default);
        result.Id.ShouldBe(category.Id);
        result.Code.ShouldBe("Fertiliser");
        result.Description.ShouldBe("Changed");
        result.DisplayOrder.ShouldBe(8);
        fixture.Inventory.Verify(repository => repository.Add(It.Is<AuditEvent>(audit => audit.Action == "Updated")), Times.Once);
        fixture.Inventory.Verify(repository => repository.Add(It.IsAny<StockMovement>()), Times.Never);
        fixture.Inventory.Verify(repository => repository.Add(It.IsAny<OperationalCostPosting>()), Times.Never);
    }

    [Test]
    public async Task StaleUpdateRejectedBeforeMutation()
    {
        Fixture fixture = new();
        await Should.ThrowAsync<ConflictException>(() => fixture.Update.Handle(new(fixture.Category.Id, "Renamed", null, 0, 99), default));
        fixture.Category.Name.ShouldBe("Fertiliser");
        fixture.Inventory.Verify(repository => repository.SaveChangesAsync(default), Times.Never);
    }

    [Test]
    public async Task AnotherTenantCategoryCannotBeUpdated()
    {
        Fixture fixture = new();
        await Should.ThrowAsync<NotFoundException>(() => fixture.Update.Handle(new(Guid.NewGuid(), "Renamed", null, 0, 1), default));
    }

    [Test]
    public async Task ListUsesCurrentTenantAndIncludesInactiveReferences()
    {
        Fixture fixture = new(); fixture.Category.SetActive(false, 1);
        IReadOnlyList<InventoryCategoryDto> result = await new GetInventoryCategoriesQueryHandler(
            fixture.Farms.Object, fixture.Inventory.Object, fixture.User.Object).Handle(new(), default);
        result.Single(category => category.Id == fixture.Category.Id).Active.ShouldBeFalse();
        fixture.Inventory.Verify(repository => repository.GetCategoriesAsync(fixture.Tenant.Id, false, default), Times.Once);
    }

    [Test]
    public async Task StatusCommandsAuditBothTransitionsWithoutStockChanges()
    {
        Fixture fixture = new();
        SetInventoryCategoryActiveCommandHandler handler = new(fixture.Farms.Object, fixture.Inventory.Object,
            fixture.User.Object, TimeProvider.System);
        (await handler.Handle(new(fixture.Category.Id, false, 1), default)).Active.ShouldBeFalse();
        (await handler.Handle(new(fixture.Category.Id, true, 2), default)).Active.ShouldBeTrue();
        fixture.Inventory.Verify(repository => repository.Add(It.Is<AuditEvent>(audit => audit.Action == "Deactivated")), Times.Once);
        fixture.Inventory.Verify(repository => repository.Add(It.Is<AuditEvent>(audit => audit.Action == "Activated")), Times.Once);
        fixture.Inventory.Verify(repository => repository.Add(It.IsAny<StockMovement>()), Times.Never);
    }

    private sealed class Fixture
    {
        public Tenant Tenant { get; }
        public InventoryCategory Category => Tenant.InventoryCategories.Single(category => category.Code == "Fertiliser");
        public Mock<IFarmSetupRepository> Farms { get; } = new();
        public Mock<IInventoryRepository> Inventory { get; } = new();
        public Mock<IUser> User { get; } = new();
        public CreateInventoryCategoryCommandHandler Create => new(Farms.Object, Inventory.Object, User.Object, TimeProvider.System);
        public UpdateInventoryCategoryCommandHandler Update => new(Farms.Object, Inventory.Object, User.Object, TimeProvider.System);

        public Fixture(string role = TenantSecurityRoles.FarmManager)
        {
            Tenant = Tenant.CreateForGrower("owner", "Synthetic", null);
            Farm farm = Tenant.CreateFarm("TEST", "Synthetic", "Address", "Location", "Synthetic", 1m, "Synthetic");
            if (role == TenantSecurityRoles.FarmManager)
                Tenant.AddMembership("manager", farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1)).Id, role);
            string userId = role == TenantSecurityRoles.Grower ? "owner" : "manager";
            User.Setup(user => user.Id).Returns(userId);
            Farms.Setup(repository => repository.GetTenantForUserAsync(userId, false, default)).ReturnsAsync(Tenant);
            Farms.Setup(repository => repository.GetTenantForOperationalUserAsync(userId, false, default)).ReturnsAsync(Tenant);
            Inventory.Setup(repository => repository.GetCategoriesAsync(Tenant.Id, false, default))
                .ReturnsAsync(Tenant.InventoryCategories.ToArray());
            Inventory.Setup(repository => repository.GetCategoryAsync(Tenant.Id, Category.Id, true, default)).ReturnsAsync(Category);
        }
    }
}
