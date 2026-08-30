using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class PayrollAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 8, 30, 0, TimeSpan.Zero);

    [Test]
    public async Task FarmManagerAdvanceApprovalReturnsForbiddenBeforeRepositoryMutation()
    {
        var tenant = TenantWithManager(out _); var farms = FarmRepository(tenant, "manager-user"); var payroll = new Mock<IPayrollRepository>(); var labour = new Mock<ILabourRepository>();
        var handler = new DecideWorkerAdvanceCommandHandler(farms.Object, labour.Object, payroll.Object, User("manager-user").Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(new DecideWorkerAdvanceCommand(Guid.NewGuid(), 2, true, null, "manager-key"), CancellationToken.None));

        payroll.Verify(repository => repository.Add(It.IsAny<AdvanceApproval>()), Times.Never);
        payroll.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task FarmManagerPayrollApprovalReturnsForbiddenBeforeRepositoryMutation()
    {
        var tenant = TenantWithManager(out _); var farms = FarmRepository(tenant, "manager-user"); var payroll = new Mock<IPayrollRepository>(); var labour = new Mock<ILabourRepository>();
        var handler = new DecidePayrollRunCommandHandler(farms.Object, labour.Object, payroll.Object, User("manager-user").Object, new FixedTimeProvider(Now));
        await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(new DecidePayrollRunCommand(Guid.NewGuid(), 2, 1, true, null, "manager-payroll-key"), CancellationToken.None));
        payroll.Verify(repository => repository.Add(It.IsAny<PayrollApproval>()), Times.Never); payroll.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GrowerApprovalBindsTheExactPendingVersion()
    {
        var tenant = TenantWithManager(out var farm); var advance = PendingAdvance(tenant, farm); var farms = FarmRepository(tenant, "grower-user"); var payroll = PayrollRepository(tenant, farm, advance, out var approvals, out _); var labour = new Mock<ILabourRepository>();
        var handler = new DecideWorkerAdvanceCommandHandler(farms.Object, labour.Object, payroll.Object, User("grower-user").Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(new DecideWorkerAdvanceCommand(advance.Id, advance.Version, true, null, "grower-key"), CancellationToken.None);

        approvals.Single().AdvanceVersion.ShouldBe(2);
        approvals.Single().Approved.ShouldBeTrue();
        approvals.Single().AmountUsdSnapshot.ShouldBe(advance.RequestedAmountUsd);
        approvals.Single().InstallmentCountSnapshot.ShouldBe(advance.Installments.Count);
        approvals.Single().InstallmentScheduleSnapshot.ShouldContain(advance.Installments.First().PayrollPeriodId.ToString("N"));
        result.Status.ShouldBe("Approved");
        result.OutstandingAmountUsd.ShouldBe(0m);
        payroll.Verify(repository => repository.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task MobileMoneyIssueStoresAndReturnsOnlyMaskedRecipient()
    {
        var tenant = TenantWithManager(out var farm); var advance = PendingAdvance(tenant, farm); advance.Decide(true, advance.Version); var farms = FarmRepository(tenant, "grower-user"); var payroll = PayrollRepository(tenant, farm, advance, out _, out var issues); var labour = new Mock<ILabourRepository>();
        var handler = new IssueWorkerAdvanceCommandHandler(farms.Object, labour.Object, payroll.Object, User("grower-user").Object);
        var localIssueTime = Now.ToOffset(TimeSpan.FromHours(2));

        var result = await handler.Handle(new IssueWorkerAdvanceCommand(advance.Id, advance.Version, AdvancePaymentMethod.MobileMoney, advance.ApprovedAmountUsd!.Value, localIssueTime, null, null, "EcoCash", "0770000123", "MM-REF", "Confirmed", "issue-key"), CancellationToken.None);

        issues.Single().MaskedRecipientNumber.ShouldBe("•••• 0123");
        issues.Single().IssuedAt.ShouldBe(Now);
        result.Issue!.MaskedRecipientNumber.ShouldBe("•••• 0123");
        result.OutstandingAmountUsd.ShouldBe(advance.ApprovedAmountUsd!.Value);
        result.Issue.ShouldNotBeNull();
    }

    [Test]
    public async Task StaleGrowerApprovalReturnsConflictAndCreatesNoFact()
    {
        var tenant = TenantWithManager(out var farm); var advance = PendingAdvance(tenant, farm); var farms = FarmRepository(tenant, "grower-user"); var payroll = PayrollRepository(tenant, farm, advance, out var approvals, out _); var labour = new Mock<ILabourRepository>();
        var handler = new DecideWorkerAdvanceCommandHandler(farms.Object, labour.Object, payroll.Object, User("grower-user").Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<ConflictException>(() => handler.Handle(new DecideWorkerAdvanceCommand(advance.Id, advance.Version - 1, true, null, "stale-key"), CancellationToken.None));

        approvals.ShouldBeEmpty();
        payroll.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static WorkerAdvance PendingAdvance(Tenant tenant, Farm farm)
    {
        var advance = WorkerAdvance.Create(tenant.Id, farm.Id, Guid.NewGuid(), 100m, "Transport", new DateOnly(2026, 8, 27), Guid.NewGuid(), 3, Now, "manager-user", null);
        advance.SetSchedule([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()], advance.Version); advance.Submit(advance.Version); return advance;
    }

    private static Tenant TenantWithManager(out Farm farm)
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Grower", null); farm = tenant.CreateFarm("PAY", "Payroll farm", "Address", "Location", "Lease", 10m, "Furrow");
        var manager = farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1)); farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1)); tenant.AddFarmManagerMembership("manager-user", manager.Id); return tenant;
    }

    private static Mock<IFarmSetupRepository> FarmRepository(Tenant tenant, string userId)
    { var repository = new Mock<IFarmSetupRepository>(); repository.Setup(value => value.GetTenantForUserAsync(userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant); return repository; }

    private static Mock<IPayrollRepository> PayrollRepository(Tenant tenant, Farm farm, WorkerAdvance advance, out List<AdvanceApproval> approvals, out List<AdvanceIssue> issues)
    {
        approvals = []; issues = []; var approvalFacts = approvals; var issueFacts = issues; var repository = new Mock<IPayrollRepository>();
        repository.Setup(value => value.GetApprovalByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdvanceApproval?)null);
        repository.Setup(value => value.GetIssueByKeyAsync(tenant.Id, farm.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AdvanceIssue?)null);
        repository.Setup(value => value.GetAdvanceAsync(tenant.Id, farm.Id, advance.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(advance);
        repository.Setup(value => value.GetApprovalsAsync(tenant.Id, farm.Id, advance.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => approvalFacts.ToArray());
        repository.Setup(value => value.GetIssueAsync(tenant.Id, farm.Id, advance.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => issueFacts.SingleOrDefault());
        repository.Setup(value => value.Add(It.IsAny<AdvanceApproval>())).Callback<AdvanceApproval>(approvalFacts.Add);
        repository.Setup(value => value.Add(It.IsAny<AdvanceIssue>())).Callback<AdvanceIssue>(issueFacts.Add);
        repository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1); return repository;
    }

    private static Mock<IUser> User(string id)
    { var user = new Mock<IUser>(); user.Setup(value => value.Id).Returns(id); user.Setup(value => value.CorrelationId).Returns("p6a-unit-test"); return user; }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider { public override DateTimeOffset GetUtcNow() => value; }
}
