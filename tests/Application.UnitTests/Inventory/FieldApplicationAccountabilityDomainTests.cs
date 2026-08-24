using Cane360.Domain.Inventory;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class FieldApplicationAccountabilityDomainTests
{
    [Test]
    public void PartialFieldReceiptsPreserveIssueTraceAndRecordedReceiptCannotBeEdited()
    {
        var fixture = new AccountabilityFixture();
        var first = FieldReceipt.Create(fixture.TenantId, fixture.FarmId, fixture.Issue, fixture.FieldId,
            fixture.CycleId, fixture.ActivityId, Guid.NewGuid(), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow, "manager", null, 0);
        var second = FieldReceipt.Create(fixture.TenantId, fixture.FarmId, fixture.Issue, fixture.FieldId,
            fixture.CycleId, fixture.ActivityId, Guid.NewGuid(), DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow, "manager", null, 0);

        first.AddLine(fixture.IssueLine, 4m).Quantity.ShouldBe(4m);
        second.AddLine(fixture.IssueLine, 6m).Quantity.ShouldBe(6m);
        first.Supersede(first.Version);

        Should.Throw<InvalidOperationException>(() => first.AddLine(fixture.IssueLine, 1m));
        Should.Throw<InvalidOperationException>(() => FieldReceipt.Create(fixture.TenantId, fixture.FarmId,
            fixture.Issue, fixture.FieldId, fixture.CycleId, fixture.ActivityId, Guid.NewGuid(),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "manager", null, 3));
    }

    [Test]
    public void ApplicationRequiresSeparateAttestationAndExactVersionConfirmation()
    {
        var fixture = new AccountabilityFixture();
        var receipt = fixture.RecordReceipt(5m);
        var application = InputApplication.Create(fixture.TenantId, fixture.FarmId, fixture.ActivityId,
            DateTimeOffset.Parse("2026-08-24T08:00:00+02:00"), ApplicationCoverageBasis.FieldReportingHectares,
            2m, DateTimeOffset.UtcNow, "entry-user");
        var line = application.AddLine(receipt.Lines.Single(), fixture.IssueLine, fixture.Rule, 4m);

        line.ActualRate.ShouldBe(2m);
        Should.Throw<InvalidOperationException>(() => application.Confirm(DateTimeOffset.UtcNow, "manager", null,
            false, application.Version, "confirmation"));
        application.Attest(Guid.NewGuid(), DateTimeOffset.UtcNow, "entered-by-user", "verified", application.Version);
        application.SupervisorPersonId.ShouldNotBe(Guid.Empty);
        application.SupervisorAttestationEnteredByUserId.ShouldBe("entered-by-user");
        Should.Throw<InvalidOperationException>(() => application.Confirm(DateTimeOffset.UtcNow, "other-manager", null,
            false, 2, "confirmation"));

        application.Confirm(DateTimeOffset.UtcNow, "manager", null, false, application.Version, "confirmation");
        application.Status.ShouldBe(InputApplicationStatus.ManagerConfirmed);
        application.ManagerConfirmedByUserId.ShouldBe("manager");
        application.IsConfirmationRetry("confirmation").ShouldBeTrue();
    }

    [Test]
    public void LateConfirmationBoundaryRequiresReasonOnlyBeyondFortyEightHours()
    {
        var fixture = new AccountabilityFixture();
        var atWork = DateTimeOffset.Parse("2026-08-20T08:00:00+02:00");
        var exactBoundary = atWork.AddHours(48);
        var late = atWork.AddHours(48).AddTicks(1);
        var exact = fixture.AttestedApplication(atWork);
        var overdue = fixture.AttestedApplication(atWork);

        exact.Confirm(exactBoundary, "manager", null, false, exact.Version, "exact");
        exact.IsLateConfirmation.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => overdue.Confirm(late, "manager", null, true,
            overdue.Version, "late"));
        overdue.Confirm(late, "manager", "Supervisor confirmation was delayed", true, overdue.Version, "late");

        overdue.IsLateConfirmation.ShouldBeTrue();
        overdue.LateConfirmationReason.ShouldBe("Supervisor confirmation was delayed");
    }

    [Test]
    public void ReturnAndLossKeepLockedIssueCostAndCostReversalIsAppendOnly()
    {
        var fixture = new AccountabilityFixture();
        var stockReturn = StockReturn.Create(fixture.TenantId, fixture.FarmId, fixture.StoreId, fixture.ActivityId,
            new DateOnly(2026, 8, 24), Guid.NewGuid(), Guid.NewGuid());
        var returnLine = stockReturn.AddLine(fixture.IssueLine, 3m);
        stockReturn.MarkPosted(DateTimeOffset.UtcNow, "storekeeper", "post-return", stockReturn.Version);
        stockReturn.MarkReversed(DateTimeOffset.UtcNow, "reverse-return", stockReturn.Version);
        var loss = InventoryLoss.Create(fixture.TenantId, fixture.FarmId, fixture.ActivityId, fixture.IssueLine,
            2m, InventoryLossType.Spilled, "Container damaged", "manager");
        loss.Submit(DateTimeOffset.UtcNow, loss.Version);
        loss.Decide(ApprovalOutcome.Approved, DateTimeOffset.UtcNow, loss.Version);
        var lossCost = OperationalCostPosting.ForLoss(fixture.TenantId, fixture.FarmId, fixture.FieldId,
            fixture.ActivityId, fixture.CycleId, loss, "loss:1");
        var reversal = OperationalCostPosting.Reverse(lossCost, "loss:1:reversal");

        returnLine.IssueUnitCostUsdSnapshot.ShouldBe(3m);
        stockReturn.Status.ShouldBe(StockReturnStatus.Reversed);
        loss.Status.ShouldBe(InventoryLossStatus.Approved);
        lossCost.AmountUsd.ShouldBe(6m);
        reversal.AmountUsd.ShouldBe(-6m);
        reversal.ReversalOfOperationalCostPostingId.ShouldBe(lossCost.Id);
    }

    [Test]
    public void ControlExceptionRetainsTraceUntilExactReconciliation()
    {
        var exception = ControlException.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            10m, 4m, 3m, 2m, 1m, DateTimeOffset.UtcNow);

        exception.Status.ShouldBe(ControlExceptionStatus.Open);
        exception.UnaccountedQuantity.ShouldBe(1m);
        exception.Resolve(5m, 3m, 2m, DateTimeOffset.UtcNow);

        exception.Status.ShouldBe(ControlExceptionStatus.Resolved);
        exception.UnaccountedQuantity.ShouldBe(0m);
    }

    private sealed class AccountabilityFixture
    {
        public AccountabilityFixture()
        {
            TenantId = Guid.NewGuid(); FarmId = Guid.NewGuid(); StoreId = Guid.NewGuid(); FieldId = Guid.NewGuid();
            CycleId = Guid.NewGuid(); ActivityId = Guid.NewGuid();
            var unit = UnitOfMeasure.Create(TenantId, "kg", "Kilogram", "Mass", 6);
            var item = InventoryItem.Create(TenantId, FarmId, "FERT", "Fertiliser", InventoryItemCategory.Fertiliser,
                unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
            Rule = InventoryApplicationRule.Create(TenantId, FarmId, item, Guid.NewGuid(), new DateOnly(2026, 1, 1),
                null, ApplicationCoverageBasis.FieldReportingHectares, 2m, 0m, 0m);
            var request = InputRequest.Create(TenantId, FarmId, FieldId, CycleId, ActivityId, new DateOnly(2026, 8, 24), "manager");
            var requestLine = request.AddLine(item, Rule, 5m, 10m, 10m, 3m, request.Version);
            Issue = StockIssue.Create(TenantId, FarmId, StoreId, request.Id, new DateOnly(2026, 8, 24), Guid.NewGuid(), Guid.NewGuid(), null, 0);
            IssueLine = Issue.AddLine(requestLine, Guid.NewGuid(), null, null, 10m, Issue.Version);
            IssueLine.LockCost(3m);
            Issue.MarkPosted(DateTimeOffset.UtcNow, "storekeeper", "issue", Issue.Version);
        }

        public Guid TenantId { get; } public Guid FarmId { get; } public Guid StoreId { get; }
        public Guid FieldId { get; } public Guid CycleId { get; } public Guid ActivityId { get; }
        public StockIssue Issue { get; } public StockIssueLine IssueLine { get; }
        public InventoryApplicationRule Rule { get; }

        public FieldReceipt RecordReceipt(decimal quantity)
        {
            var receipt = FieldReceipt.Create(TenantId, FarmId, Issue, FieldId, CycleId, ActivityId, Guid.NewGuid(),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "manager", null, 0);
            receipt.AddLine(IssueLine, quantity);
            return receipt;
        }

        public InputApplication AttestedApplication(DateTimeOffset appliedAt)
        {
            var receipt = RecordReceipt(5m);
            var application = InputApplication.Create(TenantId, FarmId, ActivityId, appliedAt,
                ApplicationCoverageBasis.FieldReportingHectares, 2m, appliedAt, "entry-user");
            application.AddLine(receipt.Lines.Single(), IssueLine, Rule, 4m);
            application.Attest(Guid.NewGuid(), appliedAt, "entry-user", null, application.Version);
            return application;
        }
    }
}
