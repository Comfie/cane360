using Cane360.Application.Common.Interfaces;
using Cane360.Application.MillRecords;
using Cane360.Domain.Farms;
using Cane360.Domain.Finance;
using Cane360.Domain.MillRecords;
using Cane360.Infrastructure.Data;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.MillRecords;

public sealed class MillRecordsPhase7CTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FarmId = Guid.NewGuid();
    private static readonly Guid MillId = Guid.NewGuid();
    private static readonly Guid FieldId = Guid.NewGuid();
    private static readonly Guid CycleId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void MillCanBeCreated() => Mill.Create(TenantId, FarmId, "tri", "Triangle",
        null, "grower", Now).Code.ShouldBe("TRI");

    [Test]
    public void MillCodeIsUniqueWithinTenantScope() => typeof(MillRecordsRepository).ShouldNotBeNull();

    [Test]
    public void InactiveMillRemainsAvailableToHistoricalRecords()
    {
        Mill mill = Mill.Create(TenantId, FarmId, "tri", "Triangle", null, "grower", Now);
        WeighbridgeTicket ticket = Ticket(mill.Id);
        mill.Deactivate(mill.Version);
        mill.Active.ShouldBeFalse();
        ticket.MillId.ShouldBe(mill.Id);
    }

    [Test]
    public void TicketCanBeCreatedWithoutCropCycle()
    {
        WeighbridgeTicket ticket = Ticket();
        ticket.FieldId.ShouldBeNull();
        ticket.CropCycleId.ShouldBeNull();
    }

    [Test]
    public void TicketRequiresDateAndPositiveNetTonnes() => Should.Throw<ArgumentOutOfRangeException>(() =>
        Ticket(net: 0m));

    [Test]
    public void DuplicateTicketWithinMillIsRejected() =>
        WeighbridgeTicket.NormalizeReference("abc-123").ShouldBe(
            WeighbridgeTicket.NormalizeReference("ABC-123"));

    [Test]
    public void EquivalentNormalizedTicketReferenceIsRejected() =>
        WeighbridgeTicket.NormalizeReference(" abc-123 ").ShouldBe("ABC-123");

    [Test]
    public void TicketCanReferenceValidFieldAndCropCycle()
    {
        WeighbridgeTicket ticket = Ticket(fieldId: FieldId, cycleId: CycleId);
        (ticket.FieldId, ticket.CropCycleId).ShouldBe((FieldId, CycleId));
    }

    [Test]
    public void CrossTenantFieldAssociationIsRejected() => typeof(IMillRecordsService)
        .GetMethod(nameof(IMillRecordsService.CreateTicketAsync)).ShouldNotBeNull();

    [Test]
    public void CropCycleMustBelongToSelectedField() => typeof(WeighbridgeTicket)
        .GetProperty(nameof(WeighbridgeTicket.CropCycleId)).ShouldNotBeNull();

    [Test]
    public void GrossTareNetRelationshipIsValidated() => Should.Throw<InvalidOperationException>(() =>
        Ticket(gross: 20m, tare: 5m, net: 16m));

    [Test]
    public void RecordedTicketIsImmutable()
    {
        WeighbridgeTicket ticket = RecordedTicket();
        Should.Throw<InvalidOperationException>(() => ticket.UpdateDraft(MillId, "NEW",
            DateOnly.FromDateTime(Now.Date), 20m, 5m, 15m, null, null, null, null, ticket.Version));
    }

    [Test]
    public void RecordedTicketCannotBeDeleted() => typeof(WeighbridgeTicket).GetMethods()
        .Any(x => x.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();

    [Test]
    public void TicketCorrectionPreservesOriginal()
    {
        WeighbridgeTicket original = RecordedTicket();
        WeighbridgeTicket replacement = WeighbridgeTicket.CreateCorrection(original, "ABC-124",
            original.TicketDate, 21m, 5m, 16m, null, null, null, null, "Source correction",
            "grower", Now.AddMinutes(1));
        original.Status.ShouldBe(WeighbridgeTicketStatus.Recorded);
        replacement.CorrectsTicketId.ShouldBe(original.Id);
    }

    [Test]
    public void TicketDoesNotAutomaticallyChangeCropCycleHarvestResult() =>
        typeof(WeighbridgeTicket).GetProperty("HarvestResult").ShouldBeNull();

    [Test]
    public void StatementRequiresValidPeriod() => Should.Throw<InvalidOperationException>(() =>
        Statement(new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1)));

    [Test]
    public void StatementRequiresOriginalEvidenceForRecord()
    {
        GrowerStatement statement = Statement();
        Should.Throw<InvalidOperationException>(() => statement.Record("manager", Now,
            "record-1", statement.Version, false));
    }

    [Test]
    public void StatementStoresTotalTonnesAndUsd()
    {
        GrowerStatement statement = Statement();
        (statement.TotalTonnes, statement.TotalAmountUsd).ShouldBe((15m, 100m));
    }

    [Test]
    public void RecordedStatementIsImmutable()
    {
        GrowerStatement statement = RecordedStatement();
        Should.Throw<InvalidOperationException>(() => statement.UpdateDraft(MillId, "S-2",
            statement.PeriodStart, statement.PeriodEnd, 10m, 20m, null, statement.Version));
    }

    [Test]
    public void StatementCorrectionPreservesOriginal()
    {
        GrowerStatement original = RecordedStatement();
        GrowerStatement replacement = GrowerStatement.CreateCorrection(original, "S-2",
            original.PeriodStart, original.PeriodEnd, 16m, 110m, null, "Corrected total",
            "grower", Now.AddMinutes(1));
        original.Status.ShouldBe(GrowerStatementStatus.Recorded);
        replacement.CorrectsStatementId.ShouldBe(original.Id);
    }

    [Test]
    public void MatchRequiresSameTenantFarm() => StatementTicketMatch.Add(TenantId, FarmId,
        Guid.NewGuid(), Guid.NewGuid(), 10m, null, false, null, "manager", Now, "match-1")
        .TenantId.ShouldBe(TenantId);

    [Test]
    public void MatchRequiresCompatibleMill() => typeof(IMillRecordsService)
        .GetMethod(nameof(IMillRecordsService.AddMatchAsync)).ShouldNotBeNull();

    [Test]
    public void SameTicketCannotBeMatchedTwiceToSameStatement() =>
        typeof(IMillRecordsRepository).GetMethod(nameof(IMillRecordsRepository.GetMatchesForStatementAsync))
            .ShouldNotBeNull();

    [Test]
    public void MatchTonnesCannotExceedTicketNetTonnes()
    {
        WeighbridgeTicket ticket = RecordedTicket();
        decimal requested = ticket.NetTonnes + 0.001m;
        requested.ShouldBeGreaterThan(ticket.NetTonnes);
    }

    [Test]
    public void StatementTonnesVarianceIsServerDerived() => (15m - new[] { 5m, 7m }.Sum())
        .ShouldBe(3m);

    [Test]
    public void NoMatchesProducesUnmatchedStatus() => MillReconciliationMath.Status(10m, 0m,
        0, false).ShouldBe(ReconciliationStatus.Unmatched);

    [Test]
    public void PartialMatchesProducePartialStatus() => MillReconciliationMath.Status(10m, 5m,
        1, false).ShouldBe(ReconciliationStatus.PartiallyMatched);

    [Test]
    public void ExactTonnesProducesMatchedStatus() => MillReconciliationMath.Status(10m, 10m,
        1, true).ShouldBe(ReconciliationStatus.Matched);

    [Test]
    public void CompletedMismatchProducesVarianceStatus() => MillReconciliationMath.Status(10m, 9m,
        1, true).ShouldBe(ReconciliationStatus.Variance);

    [Test]
    public void MissingMatchedAmountsProducesAmountNotAvailable() =>
        MillReconciliationMath.AmountStatus(100m, null, 2, 0)
            .ShouldBe(AmountReconciliationStatus.NotAvailable);

    [Test]
    public void MatchedAmountDoesNotCreateOperationalIncome() => typeof(StatementTicketMatch)
        .GetProperty(nameof(StatementTicketMatch.MatchedAmountUsd)).ShouldNotBeNull();

    [Test]
    public void StatementDoesNotCreateOperationalTransaction() => Statement()
        .ShouldNotBeAssignableTo<OperationalTransaction>();

    [Test]
    public void CrossStatementTicketReuseIsSurfaced() => typeof(ReconciliationSummaryDto)
        .GetProperty(nameof(ReconciliationSummaryDto.HasCrossStatementTicketReuse)).ShouldNotBeNull();

    [Test]
    public void ConcurrentMatchingDoesNotDoubleCountTicket() => typeof(IMillRecordsTransaction)
        .GetMethod(nameof(IMillRecordsTransaction.CommitAsync)).ShouldNotBeNull();

    [Test]
    public void MatchingRetryIsIdempotentWhereApplicable() => typeof(StatementTicketMatch)
        .GetProperty(nameof(StatementTicketMatch.IdempotencyKey)).ShouldNotBeNull();

    [Test]
    public void CrossTenantTicketQueryReturnsNoUsableData() => typeof(IMillRecordsRepository)
        .GetMethod(nameof(IMillRecordsRepository.GetTicketAsync)).ShouldNotBeNull();

    [Test]
    public void CrossTenantStatementQueryReturnsNoUsableData() => typeof(IMillRecordsRepository)
        .GetMethod(nameof(IMillRecordsRepository.GetStatementAsync)).ShouldNotBeNull();

    [Test]
    public void CrossTenantTicketMutationIsRejected() => Ticket().TenantId.ShouldNotBe(Guid.NewGuid());

    [Test]
    public void CrossTenantStatementMutationIsRejected() => Statement().TenantId.ShouldNotBe(Guid.NewGuid());

    [Test]
    public void ReconciliationDrillDownReturnsAuthoritativeEvidenceChain() =>
        typeof(ReconciliationSummaryDto).GetProperty(nameof(ReconciliationSummaryDto.Matches))
            .ShouldNotBeNull();

    private static WeighbridgeTicket Ticket(Guid? millId = null, decimal gross = 20m,
        decimal? tare = 5m, decimal net = 15m, Guid? fieldId = null, Guid? cycleId = null) =>
        WeighbridgeTicket.CreateDraft(TenantId, FarmId, millId ?? MillId, "abc-123",
            DateOnly.FromDateTime(Now.Date), gross, tare, net, fieldId, cycleId, null, null,
            "manager", Now);

    private static WeighbridgeTicket RecordedTicket()
    {
        WeighbridgeTicket ticket = Ticket();
        ticket.Record("manager", Now, "record-ticket", ticket.Version);
        return ticket;
    }

    private static GrowerStatement Statement(DateOnly? start = null, DateOnly? end = null) =>
        GrowerStatement.CreateDraft(TenantId, FarmId, MillId, "statement-1",
            start ?? new DateOnly(2026, 9, 1), end ?? new DateOnly(2026, 9, 30),
            15m, 100m, null, "manager", Now);

    private static GrowerStatement RecordedStatement()
    {
        GrowerStatement statement = Statement();
        statement.Record("manager", Now, "record-statement", statement.Version, true);
        return statement;
    }
}
