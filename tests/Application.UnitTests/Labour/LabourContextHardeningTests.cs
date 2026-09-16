using Cane360.Application.Common.Interfaces;
using Cane360.Application.Labour;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public sealed class LabourContextHardeningTests
{
    [Test]
    public async Task WorkerRegisterUsesPeopleContextWithoutOperationalHistory()
    {
        const string userId = "AUTOTEST-P8A-worker-list";
        Tenant tenant = Tenant.CreateForGrower(userId, "AUTOTEST-P8A", null);
        Farm farm = tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic", "Synthetic",
            "Other", 10m, "Synthetic");
        Person person = farm.AddPerson("Synthetic Worker", null, new DateOnly(2041, 1, 1));
        WorkerProfile worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, person.Id,
            EmploymentType.Casual, new DateOnly(2041, 1, 1), new byte[16], new byte[12],
            new byte[16], "AUTOTEST-P8A-key", new byte[32], "SYNTHETIC-MASK");
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantPeopleContextForUserAsync(userId, false,
            CancellationToken.None)).ReturnsAsync(tenant);
        var labour = new Mock<ILabourRepository>(MockBehavior.Strict);
        labour.Setup(x => x.GetWorkersAsync(tenant.Id, farm.Id, false, CancellationToken.None))
            .ReturnsAsync([worker]);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);

        IReadOnlyList<WorkerListItemDto> result = await new GetWorkersQueryHandler(
            farms.Object, labour.Object, user.Object).Handle(new GetWorkersQuery(),
            CancellationToken.None);

        result.Single().DisplayName.ShouldBe("Synthetic Worker");
        farms.Verify(x => x.GetTenantForUserAsync(It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
