using Cane360.Application.Labour;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/attendance")]
public sealed class AttendanceController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AttendanceRegisterDto>> Get([FromQuery] string workDate, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(workDate, out var parsedDate))
        {
            return BadRequest(DateError(nameof(workDate)));
        }

        return Ok(await sender.Send(new GetAttendanceRegisterQuery(parsedDate), cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<AttendanceRegisterDto>> Record(RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.WorkDate, out var workDate))
        {
            return BadRequest(DateError(nameof(request.WorkDate)));
        }

        return Ok(await sender.Send(new RecordAttendanceCommand(workDate, request.LateEntryReason,
            request.Entries.Select(entry => new AttendanceEntryCommand(entry.WorkerId, entry.Status, entry.FieldId, entry.ExpectedVersion)).ToArray()), cancellationToken));
    }

    private static ValidationProblemDetails DateError(string propertyName) => new(
        new Dictionary<string, string[]> { [propertyName] = ["Date must use yyyy-MM-dd."] });
}
