using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Data;
using Cane360.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Web.UnitTests.Infrastructure;

public sealed class PayrollAppendOnlyInterceptorTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void AdvanceApprovalUpdateAndDeleteAreRejectedBeforeDatabaseAccess(bool delete)
    {
        var advance = CreateAdvance();
        var approval = AdvanceApproval.Create(
            advance.Id,
            advance.TenantId,
            advance.FarmId,
            advance.Version,
            advance.RequestedAmountUsd,
            advance.Installments,
            true,
            "grower",
            DateTimeOffset.UtcNow,
            null,
            "approval-key");
        using var context = Context();
        context.Attach(approval);
        context.Entry(approval).State = delete ? EntityState.Deleted : EntityState.Modified;
        Should.Throw<InvalidOperationException>(() => context.SaveChanges()).Message.ShouldContain("append-only");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AdvanceIssueUpdateAndDeleteAreRejectedBeforeDatabaseAccess(bool delete)
    {
        var issue = AdvanceIssue.Cash(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, DateTimeOffset.UtcNow, "manager", Guid.NewGuid(), Guid.NewGuid(), true, "issue-key");
        using var context = Context();
        context.Attach(issue);
        context.Entry(issue).State = delete ? EntityState.Deleted : EntityState.Modified;
        Should.Throw<InvalidOperationException>(() => context.SaveChanges()).Message.ShouldContain("append-only");
    }

    private static WorkerAdvance CreateAdvance()
    {
        var advance = WorkerAdvance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10m, "Test", new DateOnly(2028, 1, 1), Guid.NewGuid(), 1, DateTimeOffset.UtcNow, "manager", null);
        advance.SetSchedule([advance.RecoveryStartPayrollPeriodId], advance.Version);
        advance.Submit(advance.Version);
        return advance;
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql()
        .AddInterceptors(new AppendOnlyEntityInterceptor())
        .Options);
}
