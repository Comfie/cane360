using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.MillRecords;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Administration;

public sealed class AdministrationReferenceTests
{
    [Test]
    public void FarmSettingUsesSupportedTypedKey()
    {
        FarmSetting setting = FarmSetting.Create(Guid.NewGuid(), Guid.NewGuid(),
            FarmSetting.ActivityLateEntryReasonDays, 3, new DateOnly(2026, 10, 1), null);

        setting.Value.ShouldBe(3);
        setting.IsEffective(new DateOnly(2026, 10, 1)).ShouldBeTrue();
        setting.IsEffective(new DateOnly(2026, 9, 30)).ShouldBeFalse();
    }

    [Test]
    public void ArbitraryUnsupportedSettingKeyIsRejected()
    {
        Should.Throw<InvalidOperationException>(() => FarmSetting.Create(Guid.NewGuid(),
            Guid.NewGuid(), "ApprovalBypass", 1, new DateOnly(2026, 10, 1), null));
    }

    [Test]
    public void HistoricalSettingVersionIsPreservedWhenEnded()
    {
        FarmSetting original = FarmSetting.Create(Guid.NewGuid(), Guid.NewGuid(),
            FarmSetting.ActivityLateEntryReasonDays, 2, new DateOnly(2026, 1, 1), null);

        original.End(new DateOnly(2026, 9, 30), 1);

        original.Value.ShouldBe(2);
        original.IsEffective(new DateOnly(2026, 9, 29)).ShouldBeTrue();
        original.IsEffective(new DateOnly(2026, 10, 1)).ShouldBeFalse();
    }

    [Test]
    public void DocumentCategoryCanBeCreatedAndArchivedWithoutDeletingHistoricalReference()
    {
        Guid tenantId = Guid.NewGuid();
        DocumentCategory category = DocumentCategory.Create(tenantId, " mill ",
            "Mill evidence", null);
        EvidenceDocument evidence = EvidenceDocument.ForTicket(tenantId, Guid.NewGuid(),
            Guid.NewGuid(), "ticket.pdf", "application/pdf", 10, "safe-key", "user",
            DateTimeOffset.UtcNow);
        evidence.Classify(category);

        category.Archive(1);

        category.Active.ShouldBeFalse();
        evidence.DocumentCategoryId.ShouldBe(category.Id);
        evidence.DocumentCategoryCodeSnapshot.ShouldBe("MILL");
    }

    [Test]
    public void CrossTenantDocumentCategoryCannotClassifyEvidence()
    {
        DocumentCategory category = DocumentCategory.Create(Guid.NewGuid(), "MILL", "Mill", null);
        EvidenceDocument evidence = EvidenceDocument.ForTicket(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "ticket.pdf", "application/pdf", 10, "safe-key", "user",
            DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => evidence.Classify(category));
        evidence.DocumentCategoryId.ShouldBeNull();
    }

    [Test]
    public void ArchivedDocumentCategoryCannotClassifyNewEvidence()
    {
        Guid tenantId = Guid.NewGuid();
        DocumentCategory category = DocumentCategory.Create(tenantId, "MILL", "Mill", null);
        category.Archive(1);
        EvidenceDocument evidence = EvidenceDocument.ForTicket(tenantId, Guid.NewGuid(),
            Guid.NewGuid(), "ticket.pdf", "application/pdf", 10, "safe-key", "user",
            DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => evidence.Classify(category));
    }

    [Test]
    public void MembershipPersonMustBelongToTenantFarm()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10, "None");

        Should.Throw<InvalidOperationException>(() =>
            tenant.AddFarmManagerMembership("manager", Guid.NewGuid()));
    }

    [Test]
    public void ApplicationUserAndPersonRemainDistinct()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        Farm farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10, "None");
        Person person = farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1));

        TenantMembership membership = tenant.AddFarmManagerMembership("manager-user", person.Id);

        membership.UserId.ShouldBe("manager-user");
        membership.PersonId.ShouldBe(person.Id);
        farm.Persons.Count.ShouldBe(1);
    }
}
