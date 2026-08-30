using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollDecisionTransactionTests
{
    private static readonly DateTimeOffset Now = new(2036, 8, 28, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task GrowerRejectionCommitsOneSerializableDecisionWithoutConsumptionOrRecovery()
    {
        var tenant = Tenant.CreateForGrower("grower", "Grower", null);
        var farm = tenant.CreateFarm("P6B", "Payroll farm", "Address", "Location", "Lease", 10m, "Furrow");
        var manager = farm.AddPerson("Manager", null, new DateOnly(2036, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2036, 1, 1));
        tenant.AddFarmManagerMembership("manager", manager.Id);
        var period = PayrollPeriod.Create(tenant.Id, farm.Id, 2036, 8, Now, "manager", manager.Id);
        period.Open(Now, "manager", manager.Id, period.Version);
        var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, Now, "manager", manager.Id);
        int calculationVersion = run.RecordCalculation(run.Version);
        var calculation = PayrollCalculation.Create(Guid.NewGuid(), run.Id, period.Id, tenant.Id, farm.Id, calculationVersion, [], [], "fingerprint", Now, "manager", manager.Id);
        run.Submit(calculationVersion, Now, "manager", run.Version);
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(repository => repository.GetTenantForUserAsync("grower", false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var labour = new Mock<ILabourRepository>();
        var transaction = new Mock<IPayrollTransaction>();
        var payroll = new Mock<IPayrollRepository>();
        PayrollApproval? decision = null;
        payroll.Setup(repository => repository.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
        payroll.Setup(repository => repository.GetPayrollApprovalByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PayrollApproval?)null);
        payroll.Setup(repository => repository.GetRunAsync(tenant.Id, farm.Id, run.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(run);
        payroll.Setup(repository => repository.GetPeriodAsync(tenant.Id, farm.Id, period.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(period);
        payroll.Setup(repository => repository.GetCalculationAsync(tenant.Id, farm.Id, run.Id, calculationVersion, It.IsAny<CancellationToken>())).ReturnsAsync(calculation);
        payroll.Setup(repository => repository.GetPayrollDecisionAsync(tenant.Id, farm.Id, run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => decision);
        payroll.Setup(repository => repository.Add(It.IsAny<PayrollApproval>())).Callback<PayrollApproval>(value => decision = value);
        payroll.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var user = new Mock<IUser>(); user.Setup(value => value.Id).Returns("grower"); user.Setup(value => value.CorrelationId).Returns("AUTOTEST-P6B-unit");
        var handler = new DecidePayrollRunCommandHandler(farms.Object, labour.Object, payroll.Object, user.Object, new FixedTimeProvider(Now));

        PayrollRunDto result = await handler.Handle(new DecidePayrollRunCommand(run.Id, run.Version, calculationVersion, false, "Verification mismatch", "reject-key"), CancellationToken.None);

        result.Status.ShouldBe("Rejected");
        decision.ShouldNotBeNull().Approved.ShouldBeFalse();
        payroll.Verify(repository => repository.Add(It.IsAny<PayrollEvidenceConsumption>()), Times.Never);
        payroll.Verify(repository => repository.Add(It.IsAny<AdvanceRecovery>()), Times.Never);
        payroll.Verify(repository => repository.SaveChangesAsync(CancellationToken.None), Times.Once);
        transaction.Verify(value => value.CommitAsync(CancellationToken.None), Times.Once);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
