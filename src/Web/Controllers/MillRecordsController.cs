using System.Text;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Models;
using Cane360.Application.MillRecords;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.MillRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/finance/mill-records")]
public sealed class MillRecordsController(IMillRecordsService records) : ControllerBase
{
    [HttpGet("session", Name = "GetMillRecordsSession")]
    public async Task<ActionResult<MillRecordsSessionDto>> GetSession(CancellationToken cancellationToken) =>
        Ok(await records.GetSessionAsync(cancellationToken));

    [HttpGet("mills", Name = "GetMills")]
    public async Task<ActionResult<IReadOnlyList<MillDto>>> GetMills(
        [FromQuery] bool includeInactive, CancellationToken cancellationToken) =>
        Ok(await records.GetMillsAsync(includeInactive, cancellationToken));

    [HttpPost("mills", Name = "CreateMill")]
    public async Task<ActionResult<MillDto>> CreateMill(MillRequest request,
        CancellationToken cancellationToken)
    {
        MillDto result = await records.CreateMillAsync(new(request.Code, request.Name,
            request.Location), cancellationToken);
        return CreatedAtAction(nameof(GetMills), result);
    }

    [HttpPut("mills/{millId:guid}", Name = "UpdateMill")]
    public async Task<ActionResult<MillDto>> UpdateMill(Guid millId, MillRequest request,
        CancellationToken cancellationToken) => Ok(await records.UpdateMillAsync(millId,
            new(request.Code, request.Name, request.Location, request.ExpectedVersion), cancellationToken));

    [HttpPost("mills/{millId:guid}/deactivate", Name = "DeactivateMill")]
    public async Task<ActionResult<MillDto>> DeactivateMill(Guid millId,
        RecordMillRecordRequest request, CancellationToken cancellationToken) => Ok(await
        records.DeactivateMillAsync(millId, new(request.ExpectedVersion), cancellationToken));

    [HttpGet("tickets", Name = "GetWeighbridgeTickets")]
    public async Task<ActionResult<WeighbridgeTicketPageDto>> GetTickets(
        [FromQuery] string? from, [FromQuery] string? to, [FromQuery] Guid? millId,
        [FromQuery] Guid? fieldId, [FromQuery] Guid? cropCycleId, [FromQuery] string? status,
        [FromQuery] string? matchStatus, [FromQuery] string? search,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!TryDates(from, to, out DateOnly? fromDate, out DateOnly? toDate)) return DateError();
        return Ok(await records.GetTicketPageAsync(new(fromDate, toDate, millId, fieldId,
            cropCycleId, status, matchStatus, search), page, pageSize, cancellationToken));
    }

    [HttpGet("tickets/{ticketId:guid}", Name = "GetWeighbridgeTicket")]
    public async Task<ActionResult<WeighbridgeTicketDto>> GetTicket(Guid ticketId,
        CancellationToken cancellationToken) => Ok(await records.GetTicketAsync(ticketId, cancellationToken));

    [HttpPost("tickets", Name = "CreateWeighbridgeTicket")]
    public async Task<ActionResult<WeighbridgeTicketDto>> CreateTicket(TicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.TicketDate, out DateOnly ticketDate)) return DateError();
        WeighbridgeTicketDto result = await records.CreateTicketAsync(TicketInput(request,
            ticketDate), cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = result.Id }, result);
    }

    [HttpPut("tickets/{ticketId:guid}", Name = "UpdateWeighbridgeTicket")]
    public async Task<ActionResult<WeighbridgeTicketDto>> UpdateTicket(Guid ticketId,
        TicketRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.TicketDate, out DateOnly ticketDate)) return DateError();
        return Ok(await records.UpdateTicketAsync(ticketId, TicketInput(request, ticketDate), cancellationToken));
    }

    [HttpPost("tickets/{ticketId:guid}/record", Name = "RecordWeighbridgeTicket")]
    public async Task<ActionResult<WeighbridgeTicketDto>> RecordTicket(Guid ticketId,
        RecordMillRecordRequest request, CancellationToken cancellationToken) => Ok(await
        records.RecordTicketAsync(ticketId, new(request.ExpectedVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("tickets/{ticketId:guid}/corrections", Name = "CorrectWeighbridgeTicket")]
    public async Task<ActionResult<WeighbridgeTicketDto>> CorrectTicket(Guid ticketId,
        CorrectTicketRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.Replacement.TicketDate,
            out DateOnly ticketDate)) return DateError();
        return Ok(await records.CorrectTicketAsync(ticketId, new(request.Reason,
            request.IdempotencyKey, TicketInput(request.Replacement, ticketDate)), cancellationToken));
    }

    [HttpPost("tickets/{ticketId:guid}/evidence", Name = "UploadWeighbridgeTicketEvidence")]
    public async Task<ActionResult<EvidenceDocumentDto>> UploadTicketEvidence(Guid ticketId,
        EvidenceUploadRequest request, CancellationToken cancellationToken)
    {
        if (!TryEvidence(request, out byte[] content)) return EvidenceError();
        await using var stream = new MemoryStream(content, false);
        return Ok(await records.UploadTicketEvidenceAsync(ticketId, new(stream, request.FileName,
            request.ContentType, content.LongLength), cancellationToken));
    }

    [HttpGet("statements", Name = "GetGrowerStatements")]
    public async Task<ActionResult<GrowerStatementPageDto>> GetStatements(
        [FromQuery] string? from, [FromQuery] string? to, [FromQuery] Guid? millId,
        [FromQuery] string? matchStatus, [FromQuery] string? search,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!TryDates(from, to, out DateOnly? fromDate, out DateOnly? toDate)) return DateError();
        return Ok(await records.GetStatementPageAsync(new(fromDate, toDate, millId,
            matchStatus, search), page, pageSize, cancellationToken));
    }

    [HttpGet("statements/{statementId:guid}", Name = "GetGrowerStatement")]
    public async Task<ActionResult<GrowerStatementDto>> GetStatement(Guid statementId,
        CancellationToken cancellationToken) => Ok(await records.GetStatementAsync(statementId,
            cancellationToken));

    [HttpPost("statements", Name = "CreateGrowerStatement")]
    public async Task<ActionResult<GrowerStatementDto>> CreateStatement(StatementRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryStatementDates(request, out DateOnly start, out DateOnly end)) return DateError();
        GrowerStatementDto result = await records.CreateStatementAsync(StatementInput(request,
            start, end), cancellationToken);
        return CreatedAtAction(nameof(GetStatement), new { statementId = result.Id }, result);
    }

    [HttpPut("statements/{statementId:guid}", Name = "UpdateGrowerStatement")]
    public async Task<ActionResult<GrowerStatementDto>> UpdateStatement(Guid statementId,
        StatementRequest request, CancellationToken cancellationToken)
    {
        if (!TryStatementDates(request, out DateOnly start, out DateOnly end)) return DateError();
        return Ok(await records.UpdateStatementAsync(statementId, StatementInput(request,
            start, end), cancellationToken));
    }

    [HttpPost("statements/{statementId:guid}/record", Name = "RecordGrowerStatement")]
    public async Task<ActionResult<GrowerStatementDto>> RecordStatement(Guid statementId,
        RecordMillRecordRequest request, CancellationToken cancellationToken) => Ok(await
        records.RecordStatementAsync(statementId, new(request.ExpectedVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("statements/{statementId:guid}/corrections", Name = "CorrectGrowerStatement")]
    public async Task<ActionResult<GrowerStatementDto>> CorrectStatement(Guid statementId,
        CorrectStatementRequest request, CancellationToken cancellationToken)
    {
        if (!TryStatementDates(request.Replacement, out DateOnly start, out DateOnly end)) return DateError();
        return Ok(await records.CorrectStatementAsync(statementId, new(request.Reason,
            request.IdempotencyKey, StatementInput(request.Replacement, start, end)), cancellationToken));
    }

    [HttpPost("statements/{statementId:guid}/evidence", Name = "UploadGrowerStatementEvidence")]
    public async Task<ActionResult<EvidenceDocumentDto>> UploadStatementEvidence(Guid statementId,
        EvidenceUploadRequest request, CancellationToken cancellationToken)
    {
        if (!TryEvidence(request, out byte[] content)) return EvidenceError();
        await using var stream = new MemoryStream(content, false);
        return Ok(await records.UploadStatementEvidenceAsync(statementId, new(stream,
            request.FileName, request.ContentType, content.LongLength), cancellationToken));
    }

    [HttpGet("evidence/{evidenceId:guid}", Name = "DownloadMillRecordEvidence")]
    public async Task<IActionResult> DownloadEvidence(Guid evidenceId,
        CancellationToken cancellationToken)
    {
        EvidenceDownload download = await records.OpenEvidenceAsync(evidenceId, cancellationToken);
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpGet("statements/{statementId:guid}/candidates", Name = "GetStatementCandidateTickets")]
    public async Task<ActionResult<IReadOnlyList<CandidateTicketDto>>> GetCandidates(Guid statementId,
        CancellationToken cancellationToken) => Ok(await records.GetCandidateTicketsAsync(statementId,
            cancellationToken));

    [HttpPost("statements/{statementId:guid}/matches", Name = "AddStatementTicketMatch")]
    public async Task<ActionResult<ReconciliationSummaryDto>> AddMatch(Guid statementId,
        AddStatementTicketMatchRequest request, CancellationToken cancellationToken) => Ok(await
        records.AddMatchAsync(statementId, new(request.WeighbridgeTicketId, request.MatchedTonnes,
            request.MatchedAmountUsd, request.CompletesMatching, request.Reason,
            request.IdempotencyKey, request.ExpectedStatementVersion,
            request.ExpectedTicketVersion), cancellationToken));

    [HttpPost("statements/{statementId:guid}/matches/{matchId:guid}/reverse", Name = "ReverseStatementTicketMatch")]
    public async Task<ActionResult<ReconciliationSummaryDto>> ReverseMatch(Guid statementId,
        Guid matchId, ReverseStatementTicketMatchRequest request,
        CancellationToken cancellationToken) => Ok(await records.ReverseMatchAsync(statementId,
            matchId, new(request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpGet("statements/{statementId:guid}/reconciliation", Name = "GetStatementReconciliation")]
    public async Task<ActionResult<ReconciliationSummaryDto>> GetReconciliation(Guid statementId,
        CancellationToken cancellationToken) => Ok(await records.GetReconciliationAsync(statementId,
            cancellationToken));

    [HttpGet("tickets/export", Name = "ExportWeighbridgeRegister")]
    public async Task<IActionResult> ExportTickets([FromQuery] string? from, [FromQuery] string? to,
        [FromQuery] Guid? millId, [FromQuery] Guid? fieldId, [FromQuery] Guid? cropCycleId,
        [FromQuery] string? status, [FromQuery] string? matchStatus, [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!TryDates(from, to, out DateOnly? fromDate, out DateOnly? toDate)) return DateError();
        IReadOnlyList<WeighbridgeTicketDto> rows = await records.GetTicketsAsync(new(fromDate,
            toDate, millId, fieldId, cropCycleId, status, matchStatus, search), cancellationToken);
        ReportExportContext export = await records.RecordExportAsync("WeighbridgeRegister", Request.QueryString.Value ?? string.Empty,
            cancellationToken);
        var csv = ExportHeader(export);
        csv.AppendLine("Mill,Ticket,Date,Net tonnes,Field,Crop cycle,Match state");
        foreach (WeighbridgeTicketDto row in rows.Where(x => x.IsCurrent)) csv.AppendLine(string.Join(',',
            Csv(row.MillName), Csv(row.TicketReference), Csv(row.TicketDate), CsvCell.Number(row.NetTonnes),
            Csv(row.FieldName), Csv(row.CropCycleLabel), row.MatchedStatementIds.Count == 0 ? "Unmatched" : "Matched"));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "weighbridge-register.csv");
    }

    [HttpGet("statements/export", Name = "ExportStatementReconciliation")]
    public async Task<IActionResult> ExportStatements([FromQuery] string? from, [FromQuery] string? to,
        [FromQuery] Guid? millId, [FromQuery] string? matchStatus, [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!TryDates(from, to, out DateOnly? fromDate, out DateOnly? toDate)) return DateError();
        IReadOnlyList<GrowerStatementDto> rows = await records.GetStatementsAsync(new(fromDate,
            toDate, millId, matchStatus, search), cancellationToken);
        ReportExportContext export = await records.RecordExportAsync("StatementReconciliation", Request.QueryString.Value ?? string.Empty,
            cancellationToken);
        var csv = ExportHeader(export);
        csv.AppendLine("Statement,Period start,Period end,Mill,Statement tonnes,Matched tonnes,Tonnes variance,Statement USD,Matched USD,Amount variance,Status");
        foreach (GrowerStatementDto row in rows.Where(x => x.IsCurrent)) csv.AppendLine(string.Join(',',
            Csv(row.StatementReference), Csv(row.PeriodStart), Csv(row.PeriodEnd), Csv(row.MillName),
            CsvCell.Number(row.TotalTonnes), CsvCell.Number(row.Reconciliation.MatchedTicketTonnes), CsvCell.Number(row.Reconciliation.TonnesVariance),
            CsvCell.Number(row.TotalAmountUsd), CsvCell.Number(row.Reconciliation.MatchedAmountUsd),
            CsvCell.Number(row.Reconciliation.AmountVarianceUsd), Csv(row.Reconciliation.Status)));
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "statement-reconciliation.csv");
    }

    private static StringBuilder ExportHeader(ReportExportContext context) => new StringBuilder()
        .AppendLine($"Report,{Csv(context.Report)}")
        .AppendLine($"Farm,{Csv(context.Farm)}")
        .AppendLine($"Filters,{Csv(string.IsNullOrEmpty(context.Filters) ? "All authorized current records; all dates" : context.Filters)}")
        .AppendLine($"Generated UTC,{Csv(context.GeneratedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture))}")
        .AppendLine($"Source,{Csv(context.Source)}");

    private static TicketInput TicketInput(TicketRequest request, DateOnly date) => new(
        request.MillId, request.TicketReference, date, request.GrossTonnes, request.TareTonnes,
        request.NetTonnes, request.FieldId, request.CropCycleId, request.SourceReference,
        request.Notes, request.ExpectedVersion);
    private static StatementInput StatementInput(StatementRequest request, DateOnly start,
        DateOnly end) => new(request.MillId, request.StatementReference, start, end,
        request.TotalTonnes, request.TotalAmountUsd, request.Notes, request.ExpectedVersion);
    private static bool TryStatementDates(StatementRequest request, out DateOnly start,
        out DateOnly end)
    {
        start = default;
        end = default;
        return TransportValueParser.TryParseDateOnly(request.PeriodStart, out start) &&
            TransportValueParser.TryParseDateOnly(request.PeriodEnd, out end);
    }
    private static bool TryDates(string? from, string? to, out DateOnly? fromDate,
        out DateOnly? toDate)
    {
        fromDate = null;
        toDate = null;
        if (!string.IsNullOrWhiteSpace(from))
        { if (!TransportValueParser.TryParseDateOnly(from, out DateOnly parsed)) return false; fromDate = parsed; }
        if (!string.IsNullOrWhiteSpace(to))
        { if (!TransportValueParser.TryParseDateOnly(to, out DateOnly parsed)) return false; toDate = parsed; }
        return true;
    }
    private BadRequestObjectResult DateError() => BadRequest(new ValidationProblemDetails(
        new Dictionary<string, string[]> { ["date"] = ["Dates must use yyyy-MM-dd."] }));
    private BadRequestObjectResult EvidenceError() => BadRequest(new ValidationProblemDetails(
        new Dictionary<string, string[]> { ["evidence"] = ["Evidence must be valid base64 content no larger than 20 MB."] }));
    private static bool TryEvidence(EvidenceUploadRequest request, out byte[] content)
    {
        content = [];
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType) ||
            string.IsNullOrWhiteSpace(request.ContentBase64) || request.ContentBase64.Length > 28_000_000) return false;
        try { content = Convert.FromBase64String(request.ContentBase64); }
        catch (FormatException) { return false; }
        return content.LongLength is > 0 and <= 20 * 1024 * 1024;
    }
    private static string Csv(string? value) => CsvCell.Text(value);
}
