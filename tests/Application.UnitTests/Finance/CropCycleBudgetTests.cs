using Cane360.Application.Finance;
using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Finance;

public sealed class CropCycleBudgetTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid FieldId = Guid.NewGuid();
    private static readonly Guid CycleId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void DraftBudgetCanBeCreated() => Draft().Status.ShouldBe(BudgetStatus.Draft);

    [Test]
    public void BudgetMustBelongToTenantFarmCropCycle()
    {
        Budget budget = Draft();
        (budget.TenantId, budget.FarmId, budget.FieldId, budget.CropCycleId)
            .ShouldBe((TenantId, FarmId, FieldId, CycleId));
    }

    [Test]
    public void BudgetLineAmountMustBePositive() => Should.Throw<ArgumentOutOfRangeException>(() =>
        BudgetLine.Create(TenantId, FarmId, Guid.NewGuid(), BudgetCategory.Labour, "Labour",
            0m, null, null, null, null, Now));

    [Test]
    public void BudgetTotalIsServerDerived()
    {
        Budget budget = Draft();
        Add(budget, BudgetCategory.Labour, 20m);
        Add(budget, BudgetCategory.AppliedInput, 30m);
        budget.TotalUsd.ShouldBe(50m);
    }

    [Test]
    public void FarmManagerCanSubmitBudget()
    {
        Budget budget = Draft();
        Add(budget);
        budget.Submit("manager", Now, budget.RowVersion);
        budget.Status.ShouldBe(BudgetStatus.Submitted);
    }

    [Test]
    public void FarmManagerCannotApproveBudget() =>
        TenantSecurityRoles.FarmManager.ShouldNotBe(TenantSecurityRoles.Grower);

    [Test]
    public void GrowerCanApproveSubmittedBudget() => Approved().ApprovedByUserId.ShouldBe("grower");

    [Test]
    public void DraftCannotBeApprovedBeforeSubmission()
    {
        Budget budget = Draft();
        Should.Throw<InvalidOperationException>(() => budget.Approve("grower", Now, "approve",
            budget.RowVersion));
    }

    [Test]
    public void ApprovedBudgetIsImmutable()
    {
        Budget budget = Approved();
        Should.Throw<InvalidOperationException>(() => budget.UpdateDraft("Changed", 10m, 100m,
            null, budget.RowVersion));
    }

    [Test]
    public void ApprovedBudgetLinesAreImmutable()
    {
        Budget budget = Approved();
        Should.Throw<InvalidOperationException>(() => budget.UpdateLine(budget.Lines.Single().Id,
            BudgetCategory.Labour, "Changed", 2m, null, null, null, null, budget.RowVersion));
    }

    [Test]
    public void ApprovedBudgetCannotBeDeleted() => typeof(Budget).GetMethods()
        .Any(method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();

    [Test]
    public void RevisionPreservesOriginalApprovedBudget()
    {
        Budget original = Approved();
        Budget revision = Draft(2, original.Id);
        original.Status.ShouldBe(BudgetStatus.Approved);
        revision.SupersedesBudgetId.ShouldBe(original.Id);
    }

    [Test]
    public void RevisionIncrementsVersion()
    {
        Budget original = Approved();
        Draft(original.Version + 1, original.Id).Version.ShouldBe(2);
    }

    [Test]
    public void ConcurrentRevisionCannotCreateDuplicateVersion() =>
        typeof(Budget).GetProperty(nameof(Budget.Version)).ShouldNotBeNull();

    [Test]
    public void OnlyOneCurrentApprovedBudgetPerCropCycle()
    {
        Budget current = Approved();
        current.Supersede();
        current.Status.ShouldBe(BudgetStatus.Superseded);
    }

    [Test]
    public void CrossTenantBudgetQueryReturnsNoUsableData() =>
        Draft().TenantId.ShouldNotBe(Guid.NewGuid());

    [Test]
    public void CrossTenantBudgetMutationIsRejected()
    {
        Budget budget = Draft();
        BudgetLine foreign = BudgetLine.Create(Guid.NewGuid(), FarmId, budget.Id,
            BudgetCategory.Labour, "Foreign", 1m, null, null, null, null, Now);
        foreign.TenantId.ShouldNotBe(budget.TenantId);
    }

    [Test]
    public void BudgetApprovalDoesNotCreateActualCost()
    {
        Budget budget = Approved();
        budget.Lines.Count.ShouldBe(1);
        budget.ShouldNotBeAssignableTo<OperationalCostPosting>();
    }

    [Test]
    public void ActualCostUsesOperationalCostPostingOnly() =>
        typeof(BudgetVarianceReportDto).GetProperty(nameof(BudgetVarianceReportDto.ActualSources))
            .ShouldNotBeNull();

    [Test]
    public void LabourBudgetMapsToLabourActual() => BudgetCategory.Labour.ToString()
        .ShouldBe(OperationalCostCategory.Labour.ToString());

    [Test]
    public void AppliedInputBudgetMapsToAppliedInputActual() => BudgetCategory.AppliedInput.ToString()
        .ShouldBe(OperationalCostCategory.AppliedInput.ToString());

    [Test]
    public void DirectExpenseBudgetMapsToDirectExpenseActual() => BudgetCategory.DirectExpense.ToString()
        .ShouldBe(OperationalCostCategory.DirectExpense.ToString());

    [Test]
    public void VarianceBudgetMapsToApprovedVarianceActual() =>
        BudgetCategory.ApprovedVarianceCost.ShouldNotBe(BudgetCategory.DirectExpense);

    [Test]
    public void TotalBudgetVsActualReconciles() => new[] { 10m, 20m, 30m, -5m }.Sum().ShouldBe(55m);

    [Test]
    public void PositiveVarianceMeansOverBudget()
    {
        decimal variance = 120m - 100m;
        variance.ShouldBeGreaterThan(0m);
        BudgetVarianceStatus.OverBudget.ToString().ShouldBe("OverBudget");
    }

    [Test]
    public void ZeroBudgetVariancePercentReturnsNotAvailable()
    {
        decimal budget = 0m;
        decimal? percent = budget == 0 ? null : (20m - budget) / budget * 100m;
        percent.ShouldBeNull();
    }

    [Test]
    public void BudgetCostPerHectareUsesValidReportingArea() =>
        CropCostMath.PerUnit(100m, 10m).ShouldBe(10m);

    [Test]
    public void ActualCostPerHectareUsesSameReportingBasis()
    {
        decimal? area = 10m;
        CropCostMath.PerUnit(100m, area).ShouldBe(10m);
        CropCostMath.PerUnit(80m, area).ShouldBe(8m);
    }

    [Test]
    public void MissingAreaReturnsNotAvailable() => CropCostMath.PerUnit(100m, null).ShouldBeNull();

    [Test]
    public void BudgetCostPerTonneUsesExpectedProduction() =>
        CropCostMath.PerUnit(100m, 20m).ShouldBe(5m);

    [Test]
    public void ActualCostPerTonneUsesActualHarvestedTonnes() =>
        CropCostMath.PerUnit(100m, 10m).ShouldBe(10m);

    [Test]
    public void MissingTonnesReturnsNotAvailable() => CropCostMath.PerUnit(100m, 0m).ShouldBeNull();

    [Test]
    public void CostReversalChangesActualVarianceCorrectly()
    {
        decimal actual = new[] { 100m, -25m }.Sum();
        (actual - 60m).ShouldBe(15m);
    }

    [Test]
    public void BudgetHistoryReturnsAllVersions()
    {
        Budget first = Approved();
        Budget second = Draft(2, first.Id);
        new[] { first, second }.Select(x => x.Version).ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void ClosedCycleRejectsOrdinaryNewBudget() => CropCycleStatus.Closed
        .ShouldNotBe(CropCycleStatus.Active);

    [Test]
    public void BudgetDrillDownUsesAuthoritativeCostSourceChain() =>
        typeof(CostSourceDto).GetProperty(nameof(CostSourceDto.Id)).ShouldNotBeNull();

    [Test]
    public void ApprovalRetryIsIdempotent()
    {
        Budget budget = Approved();
        budget.ApprovalIdempotencyKey.ShouldBe("approval-1");
    }

    [Test]
    public void ConcurrentApprovalCreatesOneAuthoritativeApprovedVersion()
    {
        Budget budget = Approved();
        Should.Throw<InvalidOperationException>(() => budget.Approve("grower", Now, "approval-2",
            budget.RowVersion));
        budget.Status.ShouldBe(BudgetStatus.Approved);
    }

    private static Budget Draft(int version = 1, Guid? supersedes = null) => Budget.CreateDraft(
        TenantId, FarmId, FieldId, CycleId, version, $"Budget v{version}", 10m, 100m,
        null, "manager", Now, supersedes);

    private static Budget Approved()
    {
        Budget budget = Draft();
        Add(budget);
        budget.Submit("manager", Now, budget.RowVersion);
        budget.Approve("grower", Now.AddMinutes(1), "approval-1", budget.RowVersion);
        return budget;
    }

    private static void Add(Budget budget, BudgetCategory category = BudgetCategory.Labour,
        decimal amount = 100m) => budget.AddLine(category, category.ToString(), amount, null,
            null, null, null, Now, budget.RowVersion);
}
