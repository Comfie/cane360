using Ardalis.GuardClauses;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollContextHardeningTests
{
    [Test]
    public async Task RunRegisterUsesReferenceContextAndDoesNotLoadCalculationGraphs()
    {
        const string userId = "AUTOTEST-P8A-payroll";
        Tenant tenant = Tenant.CreateForGrower(userId, "AUTOTEST-P8A", null);
        Farm farm = tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic", "Synthetic",
            "Other", 10m, "Synthetic");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PayrollPeriod period = PayrollPeriod.Create(tenant.Id, farm.Id, 2041, 1, now, userId, null);
        PayrollRun run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, now, userId, null);
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantReferenceContextForUserAsync(userId, false, CancellationToken.None))
            .ReturnsAsync(tenant);
        var payroll = new Mock<IPayrollRepository>(MockBehavior.Strict);
        payroll.Setup(x => x.GetPeriodsAsync(tenant.Id, farm.Id, false, CancellationToken.None))
            .ReturnsAsync([period]);
        payroll.Setup(x => x.GetRunsAsync(tenant.Id, farm.Id, CancellationToken.None))
            .ReturnsAsync([run]);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        user.SetupGet(x => x.CorrelationId).Returns("AUTOTEST-P8A-correlation");

        IReadOnlyList<PayrollRunDto> result = await new GetPayrollRunsQueryHandler(
            farms.Object, payroll.Object, user.Object).Handle(new GetPayrollRunsQuery(),
            CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Calculation.ShouldBeNull();
        payroll.Verify(x => x.GetCalculationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        payroll.Verify(x => x.GetPayrollDecisionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        farms.Verify(x => x.GetTenantForUserAsync(It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PreflightRetainsFullOperationalHistoryContext()
    {
        const string userId = "AUTOTEST-P8A-payroll";
        Tenant tenant = Tenant.CreateForGrower(userId, "AUTOTEST-P8A", null);
        Farm farm = tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic", "Synthetic",
            "Other", 10m, "Synthetic");
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantForUserAsync(userId, false, CancellationToken.None))
            .ReturnsAsync(tenant);
        var payroll = new Mock<IPayrollRepository>(MockBehavior.Strict);
        Guid periodId = Guid.NewGuid();
        payroll.Setup(x => x.GetPeriodAsync(tenant.Id, farm.Id, periodId, false,
            CancellationToken.None)).ReturnsAsync((PayrollPeriod?)null);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        var handler = new GetPayrollPreflightQueryHandler(farms.Object,
            Mock.Of<ILabourRepository>(), payroll.Object, user.Object);

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(
            new GetPayrollPreflightQuery(periodId, null, null, null, 1, 25), CancellationToken.None));

        farms.Verify(x => x.GetTenantReferenceContextForUserAsync(It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
