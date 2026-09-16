using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Labour;
using Cane360.Application.MillRecords;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public sealed class SensitiveDataAccessHardeningTests
{
    [Test]
    public async Task FarmManagerCannotRevealNationalIdOrCreateAnAccessAudit()
    {
        const string managerId = "AUTOTEST-P8A-manager";
        Tenant tenant = Tenant.CreateForGrower("AUTOTEST-P8A-grower", "AUTOTEST-P8A", null);
        Farm farm = tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic", "Synthetic",
            "Other", 10m, "Synthetic");
        var manager = farm.AddPerson("AUTOTEST-P8A manager", null, new DateOnly(2041, 1, 1));
        tenant.AddFarmManagerMembership(managerId, manager.Id);
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantForUserAsync(managerId, false, CancellationToken.None))
            .ReturnsAsync(tenant);
        var labour = new Mock<ILabourRepository>(MockBehavior.Strict);
        var protector = new Mock<IWorkerSensitiveDataProtector>(MockBehavior.Strict);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(managerId);
        var handler = new RevealWorkerNationalIdCommandHandler(farms.Object, labour.Object,
            protector.Object, user.Object, TimeProvider.System);

        await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
            new RevealWorkerNationalIdCommand(Guid.NewGuid(), "Synthetic support reason"), CancellationToken.None));

        labour.VerifyNoOtherCalls();
        protector.VerifyNoOtherCalls();
    }

    [Test]
    public void OrdinaryEvidenceDtoNeverContainsAStorageKey()
    {
        typeof(EvidenceDocumentDto).GetProperties().Select(x => x.Name)
            .ShouldNotContain("StorageKey");
    }
}
