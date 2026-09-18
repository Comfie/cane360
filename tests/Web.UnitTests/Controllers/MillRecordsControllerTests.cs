using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using System.Text;
using Cane360.Application.MillRecords;
using Cane360.Web.Controllers;
using Cane360.Web.Models.MillRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class MillRecordsControllerTests
{
    [Test]
    public async Task TicketExportUsesScreenQueryFiltersAndAuditedContextWithProtectedCells()
    {
        var service = new Mock<IMillRecordsService>();
        Guid millId = Guid.NewGuid();
        WeighbridgeTicketDto row = Ticket(millId) with { TicketReference = "=1+1", NetTonnes = 15.25m };
        service.Setup(x => x.GetTicketsAsync(It.Is<TicketFilter>(f =>
            f.MillId == millId && f.From == new DateOnly(2041, 1, 1)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);
        var generated = new DateTimeOffset(2041, 1, 2, 3, 4, 5, TimeSpan.Zero);
        service.Setup(x => x.RecordExportAsync("WeighbridgeRegister", "?from=2041-01-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportExportContext("AUTOTEST-P8A Farm", "WeighbridgeRegister",
                "?from=2041-01-01", generated, "Authoritative mill records"));
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?from=2041-01-01");
        var controller = new MillRecordsController(service.Object) { ControllerContext = new ControllerContext { HttpContext = context } };

        IActionResult result = await controller.ExportTickets("2041-01-01", null, millId,
            null, null, null, null, null, CancellationToken.None);

        var file = result.ShouldBeOfType<FileContentResult>();
        string csv = Encoding.UTF8.GetString(file.FileContents);
        csv.ShouldContain("AUTOTEST-P8A Farm");
        csv.ShouldContain("?from=2041-01-01");
        csv.ShouldContain("2041-01-02T03:04:05");
        csv.ShouldContain("Authoritative mill records");
        csv.ShouldContain("\"'=1+1\"");
        csv.ShouldContain(",15.25,");
        file.ContentType.ShouldBe("text/csv; charset=utf-8");
        service.Verify(x => x.RecordExportAsync("WeighbridgeRegister", "?from=2041-01-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ControllerRequiresAuthenticatedUser() => typeof(MillRecordsController)
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();

    [Test]
    public async Task MillCreateReturnsDtoWithoutListLocation()
    {
        var service = new Mock<IMillRecordsService>();
        var expected = new MillDto(Guid.NewGuid(), "M-1", "Mill One", null,
            true, DateTimeOffset.UtcNow, 1);
        service.Setup(value => value.CreateMillAsync(It.IsAny<MillInput>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await new MillRecordsController(service.Object).CreateMill(
            new MillRequest("M-1", "Mill One", null, 0), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

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
