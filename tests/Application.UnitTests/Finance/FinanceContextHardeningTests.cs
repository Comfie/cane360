using Cane360.Application.Common.Interfaces;
using Cane360.Application.Finance;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Finance;

public sealed class FinanceContextHardeningTests
{
    [Test]
    public async Task TransactionPagePreservesFiltersAndAuthoritativeTotals()
    {
        const string userId = "AUTOTEST-P8A-finance";
        Tenant tenant = CreateTenant(userId);
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantReferenceContextForUserAsync(userId, false, CancellationToken.None))
            .ReturnsAsync(tenant);
        var finance = new Mock<IFinanceRepository>(MockBehavior.Strict);
        DateOnly from = new(2041, 1, 1);
        DateOnly to = new(2041, 1, 31);
        finance.Setup(x => x.GetTransactionPageAsync(tenant.Id, tenant.ActiveFarm!.Id, from, to,
            "Expense", "OtherExpense", "Posted", "AUTOTEST-P8A", 2, 50, CancellationToken.None))
            .ReturnsAsync(new FinanceTransactionPageSource([], 75, 5000m, 1250m, 3));
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        var service = new FinanceService(farms.Object, finance.Object,
            Mock.Of<IPayrollCostProjectionService>(), user.Object, TimeProvider.System);

        OperationalTransactionPageDto result = await service.GetTransactionPageAsync(
            new(from, to, "Expense", "OtherExpense", "Posted", "AUTOTEST-P8A"),
            2, 50, CancellationToken.None);

        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(50);
        result.TotalCount.ShouldBe(75);
        result.PostedExpenseUsd.ShouldBe(5000m);
        result.PostedIncomeUsd.ShouldBe(1250m);
        result.DraftCount.ShouldBe(3);
    }

    [TestCase(0, 50)]
    [TestCase(1, 0)]
    [TestCase(1, 101)]
    public async Task TransactionPageRejectsUnsafeBounds(int page, int pageSize)
    {
        var service = new FinanceService(Mock.Of<IFarmSetupRepository>(), Mock.Of<IFinanceRepository>(),
            Mock.Of<IPayrollCostProjectionService>(), Mock.Of<IUser>(), TimeProvider.System);

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
            service.GetTransactionPageAsync(new(null, null, null, null, null, null),
                page, pageSize, CancellationToken.None));
    }

    [Test]
    public async Task SessionDoesNotLoadOperationalHistory()
    {
        const string userId = "AUTOTEST-P8A-finance";
        Tenant tenant = CreateTenant(userId);
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantReferenceContextForUserAsync(userId, false, CancellationToken.None))
            .ReturnsAsync(tenant);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        var service = new FinanceService(farms.Object, Mock.Of<IFinanceRepository>(),
            Mock.Of<IPayrollCostProjectionService>(), user.Object, TimeProvider.System);

        await service.GetSessionAsync(CancellationToken.None);

        farms.Verify(x => x.GetTenantForUserAsync(It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PayrollReconciliationRetainsAuthoritativeActivityHistory()
    {
        const string userId = "AUTOTEST-P8A-finance";
        Tenant tenant = CreateTenant(userId);
        var farms = new Mock<IFarmSetupRepository>(MockBehavior.Strict);
        farms.Setup(x => x.GetTenantForUserAsync(userId, false, CancellationToken.None)).ReturnsAsync(tenant);
        var transaction = new Mock<IFinanceTransaction>();
        var finance = new Mock<IFinanceRepository>();
        finance.Setup(x => x.BeginSerializableTransactionAsync(CancellationToken.None)).ReturnsAsync(transaction.Object);
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Id).Returns(userId);
        var projection = new Mock<IPayrollCostProjectionService>(MockBehavior.Strict);
        var expected = new PayrollCostReconciliationDto(3, 1, 2);
        projection.Setup(x => x.ReconcileAsync(tenant, tenant.ActiveFarm!, user.Object, CancellationToken.None))
            .ReturnsAsync(expected);
        var service = new FinanceService(farms.Object, finance.Object, projection.Object,
            user.Object, TimeProvider.System);

        (await service.ReconcilePayrollAsync(CancellationToken.None)).ShouldBe(expected);

        transaction.Verify(x => x.CommitAsync(CancellationToken.None), Times.Once);
        farms.Verify(x => x.GetTenantReferenceContextForUserAsync(It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Tenant CreateTenant(string userId)
    {
        Tenant tenant = Tenant.CreateForGrower(userId, "AUTOTEST-P8A", null);
        tenant.CreateFarm("P8A", "AUTOTEST-P8A Farm", "Synthetic", "Synthetic", "Other", 10m, "Synthetic");
        return tenant;
    }
}
