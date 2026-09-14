using Cane360.Application.Common.Interfaces;
using Cane360.Application.MillRecords;
using Cane360.Web.Controllers;
using Cane360.Web.Models.MillRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class MillRecordsControllerTests
{
    [Test]
    public void ControllerRequiresAuthenticatedUser() => typeof(MillRecordsController)
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();

    [Test]
    public async Task InvalidTicketDateIsRejectedBeforeDispatch()
    {
        var service = new Mock<IMillRecordsService>();
        var controller = new MillRecordsController(service.Object);
        TicketRequest request = new(Guid.NewGuid(), "T-1", "14/09/2026", 20m, 5m,
            15m, null, null, null, null, 0);
        ActionResult<WeighbridgeTicketDto> result = await controller.CreateTicket(request,
            CancellationToken.None);
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        service.Verify(x => x.CreateTicketAsync(It.IsAny<TicketInput>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task TicketCreateMapsSourceValuesWithoutTenantInput()
    {
        var service = new Mock<IMillRecordsService>();
        Guid millId = Guid.NewGuid();
        WeighbridgeTicketDto expected = Ticket(millId);
        service.Setup(x => x.CreateTicketAsync(It.Is<TicketInput>(input => input.MillId == millId &&
            input.TicketDate == new DateOnly(2026, 9, 14) && input.NetTonnes == 15m &&
            input.TareTonnes == 5m), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new MillRecordsController(service.Object);
        ActionResult<WeighbridgeTicketDto> result = await controller.CreateTicket(new(millId,
            "T-1", "2026-09-14", 20m, 5m, 15m, null, null, null, null, 0),
            CancellationToken.None);
        result.Result.ShouldBeOfType<CreatedAtActionResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task InvalidEvidenceBase64IsRejectedBeforeStorage()
    {
        var service = new Mock<IMillRecordsService>();
        var controller = new MillRecordsController(service.Object);
        ActionResult<EvidenceDocumentDto> result = await controller.UploadTicketEvidence(
            Guid.NewGuid(), new("ticket.pdf", "application/pdf", "not-base64"),
            CancellationToken.None);
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        service.Verify(x => x.UploadTicketEvidenceAsync(It.IsAny<Guid>(),
            It.IsAny<EvidenceUpload>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task MatchMapsVersionsAndCompletionToServer()
    {
        var service = new Mock<IMillRecordsService>();
        Guid statementId = Guid.NewGuid(); Guid ticketId = Guid.NewGuid();
        ReconciliationSummaryDto expected = new(statementId, 10m, 10m, 0m, 100m,
            null, null, "NotAvailable", "Matched", false, []);
        service.Setup(x => x.AddMatchAsync(statementId, It.Is<AddMatchInput>(input =>
            input.WeighbridgeTicketId == ticketId && input.ExpectedStatementVersion == 2 &&
            input.ExpectedTicketVersion == 3 && input.CompletesMatching),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new MillRecordsController(service.Object);
        ActionResult<ReconciliationSummaryDto> result = await controller.AddMatch(statementId,
            new(ticketId, 10m, null, true, null, "match-key", 2, 3), CancellationToken.None);
        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    private static WeighbridgeTicketDto Ticket(Guid millId) => new(Guid.NewGuid(), millId,
        "TRI", "Triangle", "T-1", "2026-09-14", 20m, 5m, 15m, null, null,
        null, null, null, null, null, "Draft", 0, DateTimeOffset.UtcNow, null,
        null, null, null, true, [], [], false);
}
