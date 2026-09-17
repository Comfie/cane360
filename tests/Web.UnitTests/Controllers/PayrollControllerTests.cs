using Cane360.Application.Payroll;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Payroll;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Payroll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class PayrollControllerTests
{
    [Test]
    public void ControllerRequiresCookieAuthenticatedUser()
    {
        typeof(PayrollController).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
    }

    [Test]
    public async Task CreateAdvanceRejectsNonIsoEventDateBeforeDispatch()
    {
        var sender = new Mock<ISender>(); var controller = new PayrollController(sender.Object);
        var result = await controller.CreateAdvance(new CreateWorkerAdvanceRequest(Guid.NewGuid(), 10m, "Transport", "27/08/2026", Guid.NewGuid(), 3, null), CancellationToken.None);
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        sender.Verify(service => service.Send(It.IsAny<CreateWorkerAdvanceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreatePeriodReturnsDtoWithoutListLocation()
    {
        var sender = new Mock<ISender>();
        var expected = new PayrollPeriodDto(Guid.NewGuid(), 2026, 9,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), "September 2026",
            "Draft", null, null, 1);
        sender.Setup(value => value.Send(It.IsAny<CreatePayrollPeriodCommand>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new PayrollController(sender.Object).CreatePeriod(
            new CreatePayrollPeriodRequest(2026, 9), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task CreateAdvancePointsToExistingSingleAdvanceRoute()
    {
        var sender = new Mock<ISender>();
        var expected = Advance(Guid.NewGuid());
        sender.Setup(value => value.Send(It.IsAny<CreateWorkerAdvanceCommand>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new PayrollController(sender.Object).CreateAdvance(
            new CreateWorkerAdvanceRequest(expected.WorkerId, 30m, "Transport",
                "2026-08-27", expected.RecoveryStartPayrollPeriodId, 3, null),
            CancellationToken.None);

        var created = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        created.ActionName.ShouldBe(nameof(PayrollController.Advance));
        created.RouteValues!["advanceId"].ShouldBe(expected.Id);
        created.Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task RecordPaymentReturnsPaymentWithoutWorkerSettlementLocation()
    {
        var sender = new Mock<ISender>();
        var settlement = new Mock<IPayrollSettlementService>();
        var runId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var expected = new PayrollPaymentDto(Guid.NewGuid(), runId, Guid.NewGuid(),
            2, lineId, Guid.NewGuid(), "Cash", 30m, new DateOnly(2026, 9, 17),
            "Recorded", null, null, null, "grower", null, DateTimeOffset.UtcNow,
            0m, 30m, null, []);
        settlement.Setup(value => value.RecordPaymentAsync(runId,
            It.IsAny<RecordPayrollPaymentInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await new PayrollController(sender.Object, settlement.Object).RecordPayment(
            runId, new RecordPayrollPaymentRequest(2, lineId, "Cash", 30m,
                "2026-09-17", null, null, null, null, "payment-key"), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task PreflightMapsFiltersAndPaginationWithoutClientTotals()
    {
        var sender = new Mock<ISender>(); var periodId = Guid.NewGuid(); var workerId = Guid.NewGuid();
        var expected = new PayrollPreflightDto(periodId, [], 0, 0, 0, 0, 0, 2, 10, [], []);
        sender.Setup(service => service.Send(It.Is<GetPayrollPreflightQuery>(query => query.PayrollPeriodId == periodId && query.WorkerId == workerId && query.Eligible == false && query.EvidenceType == "WorkRecord" && query.Page == 2 && query.PageSize == 10), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new PayrollController(sender.Object).Preflight(periodId, workerId, false, "WorkRecord", 2, 10, CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task UpdateAdvanceMapsExactVersionAndCalendarDate()
    {
        var sender = new Mock<ISender>(); var id = Guid.NewGuid(); var recoveryId = Guid.NewGuid();
        var expected = Advance(id);
        sender.Setup(service => service.Send(It.Is<UpdateWorkerAdvanceCommand>(command => command.AdvanceId == id && command.AmountUsd == 25m && command.RequestedEventDate == new DateOnly(2026, 8, 27) && command.RecoveryStartPayrollPeriodId == recoveryId && command.InstallmentCount == 4 && command.ExpectedVersion == 7), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new PayrollController(sender.Object).UpdateAdvance(id, new UpdateWorkerAdvanceRequest(25m, "School", "2026-08-27", recoveryId, 4, 7), CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task IssueMapsRecipientInputWithoutAClientMaskedField()
    {
        var sender = new Mock<ISender>(); var id = Guid.NewGuid(); var expected = Advance(id); var issuedAt = DateTimeOffset.UtcNow;
        sender.Setup(service => service.Send(It.Is<IssueWorkerAdvanceCommand>(command => command.AdvanceId == id && command.ExpectedVersion == 3 && command.PaymentMethod == AdvancePaymentMethod.MobileMoney && command.RecipientNumber == "0770000123" && command.IdempotencyKey == "retry-key"), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new PayrollController(sender.Object).IssueAdvance(id, new IssueWorkerAdvanceRequest(3, AdvancePaymentMethod.MobileMoney, 30m, issuedAt, null, null, "EcoCash", "0770000123", "REF-1", "Confirmed", "retry-key"), CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task GrowerDecisionMapsExactRunAndCalculationVersions()
    {
        var sender = new Mock<ISender>(); var id = Guid.NewGuid(); var expected = Run(id);
        sender.Setup(service => service.Send(It.Is<DecidePayrollRunCommand>(command => command.PayrollRunId == id && command.ExpectedVersion == 7 && command.CalculationVersion == 3 && command.Approved && command.IdempotencyKey == "payroll-retry"), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await new PayrollController(sender.Object).DecideRun(id, new DecidePayrollRunRequest(7, 3, true, null, "payroll-retry"), CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    private static WorkerAdvanceDto Advance(Guid id) => new(id, Guid.NewGuid(), "Worker", 30m, null, "Reason", new DateOnly(2026, 8, 27), DateTimeOffset.UtcNow, Guid.NewGuid(), 3, "Draft", 1, 30m, [], [], null);
    private static PayrollRunDto Run(Guid id) => new(id, Guid.NewGuid(), "August 2026", "Open", "Calculated", 7, 3, null, DateTimeOffset.UtcNow, null, null, null, null, null, null, null, null, "trace");
}
