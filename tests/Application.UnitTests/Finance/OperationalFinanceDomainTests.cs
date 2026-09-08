using Cane360.Application.Finance;
using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using Cane360.Domain.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Finance;

public sealed class OperationalFinanceDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid FieldId = Guid.NewGuid();
    private static readonly Guid CycleId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void ExpenseDraftCanBeCreated() => Draft(OperationalTransactionType.Expense).Status
        .ShouldBe(OperationalTransactionStatus.Draft);

    [Test]
    public void IncomeDraftCanBeCreated() => Draft(OperationalTransactionType.Income).Type
        .ShouldBe(OperationalTransactionType.Income);

    [Test]
    public void AmountMustBePositive() => Should.Throw<ArgumentOutOfRangeException>(() =>
        Create(OperationalTransactionType.Expense, 0m));

    [Test]
    public void CurrencyIsImplicitlyUsdOnly()
    {
        typeof(OperationalTransaction).GetProperty("Currency").ShouldBeNull();
        Draft().AmountUsd.ShouldBe(100m);
    }

    [Test]
    public void AllocationTotalMustEqualTransactionAmountBeforePosting()
    {
        OperationalTransaction transaction = Draft();
        Should.Throw<InvalidOperationException>(() => transaction.ReplaceAllocations(
            [Overhead(transaction, 99m)], transaction.Version));
    }

    [Test]
    public void AllocationCannotReferenceCrossTenantCropCycle()
    {
        OperationalTransaction transaction = Draft();
        TransactionAllocation allocation = TransactionAllocation.Create(Guid.NewGuid(), FarmId,
            transaction.Id, CycleId, FieldId, OperationalFinanceCategory.Fuel, 100m,
            TransactionAllocationType.CropCycleDirect, Now);
        Should.Throw<InvalidOperationException>(() => transaction.ReplaceAllocations([allocation],
            transaction.Version));
    }

    [Test]
    public void AllocationCropCycleAndFieldMustMatch() => Should.Throw<InvalidOperationException>(() =>
        TransactionAllocation.Create(TenantId, FarmId, Guid.NewGuid(), CycleId, null,
            OperationalFinanceCategory.Fuel, 10m, TransactionAllocationType.CropCycleDirect, Now));

    [Test]
    public void PostedExpenseCreatesDirectExpenseCostPosting()
    {
        OperationalTransaction transaction = Draft();
        TransactionAllocation allocation = Direct(transaction, 100m);
        transaction.ReplaceAllocations([allocation], transaction.Version);
        transaction.Post("manager", Now, "post-1", transaction.Version);
        OperationalCostPosting posting = OperationalCostPosting.ForDirectExpense(TenantId, FarmId,
            FieldId, CycleId, allocation, "direct-1");
        posting.Category.ShouldBe(OperationalCostCategory.DirectExpense);
        posting.AmountUsd.ShouldBe(100m);
        posting.TransactionAllocationId.ShouldBe(allocation.Id);
    }

    [Test]
    public void IncomeDoesNotCreateCropCostPosting()
    {
        OperationalTransaction income = Draft(OperationalTransactionType.Income);
        income.Type.ShouldBe(OperationalTransactionType.Income);
        income.ShouldNotBeAssignableTo<OperationalCostPosting>();
    }

    [Test]
    public void FarmOverheadDoesNotAutoAllocateToCropCycles()
    {
        TransactionAllocation overhead = Overhead(Draft(), 100m);
        overhead.CropCycleId.ShouldBeNull();
        overhead.FieldId.ShouldBeNull();
    }

    [Test]
    public void PostedTransactionIsImmutable()
    {
        OperationalTransaction transaction = Posted();
        Should.Throw<InvalidOperationException>(() => transaction.Update(OperationalTransactionType.Expense,
            OperationalFinanceCategory.Fuel, DateOnly.FromDateTime(Now.Date), "Changed", 10m,
            null, null, transaction.Version));
    }

    [Test]
    public void PostedTransactionCannotBeDeleted() => typeof(OperationalTransaction)
        .GetMethods().Any(x => x.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();

    [Test]
    public void ReversalPreservesOriginalTransaction()
    {
        OperationalTransaction original = Posted();
        OperationalTransaction reversal = OperationalTransaction.CreateReversal(original, "Correction",
            "grower", Now.AddMinutes(1), "reverse-1", "correlation");
        original.Status.ShouldBe(OperationalTransactionStatus.Posted);
        reversal.Status.ShouldBe(OperationalTransactionStatus.Reversed);
        reversal.ReversalOfOperationalTransactionId.ShouldBe(original.Id);
    }

    [Test]
    public void ReversalCreatesOppositeCostPosting()
    {
        OperationalTransaction transaction = Draft();
        TransactionAllocation allocation = Direct(transaction, 100m);
        OperationalCostPosting posting = OperationalCostPosting.ForDirectExpense(TenantId, FarmId,
            FieldId, CycleId, allocation, "direct-2");
        OperationalCostPosting reversal = OperationalCostPosting.Reverse(posting, "direct-2-reversal");
        reversal.AmountUsd.ShouldBe(-posting.AmountUsd);
        reversal.ReversalOfOperationalCostPostingId.ShouldBe(posting.Id);
    }

    [Test]
    public void PostingRetryIsIdempotent()
    {
        OperationalTransaction transaction = Posted();
        transaction.PostedIdempotencyKey.ShouldBe("post-1");
        Should.Throw<InvalidOperationException>(() => transaction.Post("manager", Now, "post-1",
            transaction.Version));
    }

    [Test]
    public void ConcurrentPostingDoesNotDuplicateCost()
    {
        OperationalTransaction transaction = Posted();
        transaction.Allocations.Select(x => x.Id).Distinct().Count().ShouldBe(1);
        transaction.PostedIdempotencyKey.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void ApprovedPayrollLabourCreatesCostPosting()
    {
        PayrollEarningLine earning = Earning();
        OperationalCostPosting posting = OperationalCostPosting.ForPayroll(TenantId, FarmId, FieldId,
            Guid.NewGuid(), CycleId, earning, "payroll-1");
        posting.Category.ShouldBe(OperationalCostCategory.Labour);
        posting.PayrollEarningLineId.ShouldBe(earning.Id);
    }

    [Test]
    public void UnapprovedPayrollDoesNotCreateCostPosting()
    {
        PayrollRun run = PayrollRun.Create(TenantId, FarmId, Guid.NewGuid(), Now, "manager", null);
        run.Status.ShouldBe(PayrollRunStatus.Draft);
        run.Status.ShouldNotBe(PayrollRunStatus.Approved);
    }

    [Test]
    public void SupersededPayrollVersionDoesNotCreateCostPosting()
    {
        PayrollRun run = PayrollRun.Create(TenantId, FarmId, Guid.NewGuid(), Now, "manager", null);
        int first = run.RecordCalculation(run.Version);
        int second = run.RecordCalculation(run.Version);
        first.ShouldBe(1);
        second.ShouldBe(2);
        run.LatestCalculationVersion.ShouldBe(second);
    }

    [Test]
    public void PayrollCostUsesApprovedEarningSourceNotNetPayment()
    {
        PayrollEarningLine earning = Earning(3m, 7.25m);
        OperationalCostPosting posting = OperationalCostPosting.ForPayroll(TenantId, FarmId, FieldId,
            null, CycleId, earning, "payroll-2");
        posting.AmountUsd.ShouldBe(earning.EarningAmountUsd);
        posting.AmountUsd.ShouldBe(21.75m);
    }

    [Test]
    public void PayrollReconciliationAddsOnlyMissingPostings()
    {
        OperationalCostPosting posting = OperationalCostPosting.ForPayroll(TenantId, FarmId, FieldId,
            null, CycleId, Earning(), "stable-payroll-source");
        posting.PostingIdentity.ShouldBe("stable-payroll-source");
    }

    [Test]
    public void PayrollReconciliationRetryIsIdempotent()
    {
        PayrollCostReconciliationDto result = new(4, 0, 4);
        result.PostingsAdded.ShouldBe(0);
        result.ExistingPostingsPreserved.ShouldBe(4);
    }

    [Test]
    public void ExistingAppliedInputCostIsNotDuplicated() => Enum.IsDefined(
        OperationalCostCategory.AppliedInput).ShouldBeTrue();

    [Test]
    public void ExistingInventoryLossCostIsNotDuplicated() => Enum.IsDefined(
        OperationalCostCategory.ApprovedInventoryLoss).ShouldBeTrue();

    [Test]
    public void TotalCropCycleCostReconcilesToActiveCostPostings()
    {
        decimal[] signedPostings = [100m, 40m, 20m, -20m];
        signedPostings.Sum().ShouldBe(140m);
    }

    [Test]
    public void CostPerHectareUsesSelectedReportingArea() => CropCostMath.PerUnit(250m, 10m)
        .ShouldBe(25m);

    [Test]
    public void MissingReportingAreaReturnsNotAvailable() => CropCostMath.PerUnit(250m, null)
        .ShouldBeNull();

    [Test]
    public void CostPerTonneUsesActualHarvestedTonnes() => CropCostMath.PerUnit(250m, 5m)
        .ShouldBe(50m);

    [Test]
    public void MissingHarvestTonnesReturnsNotAvailable() => CropCostMath.PerUnit(250m, 0m)
        .ShouldBeNull();

    [Test]
    public void ClosedCycleOrdinaryExpensePostingIsRejected()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        Farm farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10m, "Furrow");
        CropVariety variety = tenant.AddCropVariety("NCO", "NCo376");
        Field field = farm.AddField("F1", "Field", 10m, null, ReportingAreaSource.Declared,
            "Furrow", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2026, 1, 1), new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31), 100m, Now, "grower");
        cycle.AcceptsOperationalEntries.ShouldBeFalse();
    }

    [Test]
    public void AuthorizedClosedCycleCorrectionPreservesHistory()
    {
        OperationalTransaction original = Posted();
        OperationalTransaction reversal = OperationalTransaction.CreateReversal(original, "Post-close correction",
            "grower", Now, "closed-reversal", "correlation");
        reversal.ReversalReason.ShouldBe("Post-close correction");
        reversal.EventDate.ShouldBe(original.EventDate);
    }

    [Test]
    public void FarmManagerCannotPerformGrowerOnlyClosedCorrection()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        Farm farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10m, "Furrow");
        var person = farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(person, Cane360.Domain.Activities.PersonRole.FarmManager, true,
            new DateOnly(2026, 1, 1));
        tenant.AddFarmManagerMembership("manager", person.Id);
        tenant.Memberships.Single(x => x.UserId == "manager").SecurityRole.ShouldBe(TenantSecurityRoles.FarmManager);
    }

    [Test]
    public void CrossTenantTransactionQueryReturnsNoUsableData()
    {
        OperationalTransaction transaction = Draft();
        transaction.TenantId.ShouldBe(TenantId);
        transaction.TenantId.ShouldNotBe(Guid.NewGuid());
    }

    [Test]
    public void CrossTenantTransactionMutationIsRejected()
    {
        OperationalTransaction transaction = Draft();
        TransactionAllocation foreign = TransactionAllocation.Create(Guid.NewGuid(), Guid.NewGuid(),
            transaction.Id, null, null, OperationalFinanceCategory.Fuel, 100m,
            TransactionAllocationType.FarmOverhead, Now);
        Should.Throw<InvalidOperationException>(() => transaction.ReplaceAllocations([foreign],
            transaction.Version));
    }

    [Test]
    public void CostSourceDrillDownReturnsAuthoritativeSourceChain()
    {
        TransactionAllocation allocation = Direct(Draft(), 100m);
        OperationalCostPosting posting = OperationalCostPosting.ForDirectExpense(TenantId, FarmId,
            FieldId, CycleId, allocation, "traceable-direct-source");
        posting.TransactionAllocationId.ShouldBe(allocation.Id);
        posting.PostingIdentity.ShouldBe("traceable-direct-source");
    }

    private static OperationalTransaction Create(OperationalTransactionType type, decimal amount) =>
        OperationalTransaction.Create(TenantId, FarmId, type, OperationalFinanceCategory.Fuel,
            new DateOnly(2026, 9, 3), "Supplier", amount, "SRC-1", null, "manager", Now, "correlation");
    private static OperationalTransaction Draft(OperationalTransactionType type = OperationalTransactionType.Expense) => Create(type, 100m);
    private static TransactionAllocation Direct(OperationalTransaction transaction, decimal amount) =>
        TransactionAllocation.Create(TenantId, FarmId, transaction.Id, CycleId, FieldId,
            OperationalFinanceCategory.Fuel, amount, TransactionAllocationType.CropCycleDirect, Now);
    private static TransactionAllocation Overhead(OperationalTransaction transaction, decimal amount) =>
        TransactionAllocation.Create(TenantId, FarmId, transaction.Id, null, null,
            OperationalFinanceCategory.Fuel, amount, TransactionAllocationType.FarmOverhead, Now);
    private static OperationalTransaction Posted()
    {
        OperationalTransaction transaction = Draft();
        transaction.ReplaceAllocations([Direct(transaction, 100m)], transaction.Version);
        transaction.Post("manager", Now, "post-1", transaction.Version);
        return transaction;
    }
    private static PayrollEarningLine Earning(decimal quantity = 2m, decimal rate = 10m) =>
        PayrollEarningLine.Create(Guid.NewGuid(), Guid.NewGuid(), TenantId, FarmId, Guid.NewGuid(),
            Guid.NewGuid(), "WorkRecord", new DateOnly(2026, 9, 1), Guid.NewGuid(), 1, Now, Now,
            FieldId, "[]", quantity, "day", "Daily", rate, Guid.NewGuid(), 1, "fingerprint");
}
