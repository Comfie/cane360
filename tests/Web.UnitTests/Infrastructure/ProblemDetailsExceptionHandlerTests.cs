using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Web.Infrastructure;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Cane360.Web.UnitTests.Infrastructure;

public class ProblemDetailsExceptionHandlerTests
{
    [Test]
    public async Task TryHandleAsyncReturnsValidationMessagesInProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ValidationException([new ValidationFailure("PayBasis", "No effective rate exists for this worker, work date, and scope.")]);

        bool handled = await new ProblemDetailsExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.Body.Position = 0;
        string response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        response.ShouldContain("No effective rate exists for this worker, work date, and scope.");
    }

    [TestCase("Forbidden", StatusCodes.Status403Forbidden, "Forbidden")]
    [TestCase("NotFound", StatusCodes.Status404NotFound, "not found")]
    [TestCase("Conflict", StatusCodes.Status409Conflict, "PayrollCalculationStale")]
    public async Task Phase6BPayrollExceptionsExposeAuthoritative403404And409Contracts(string kind, int expectedStatus, string expectedBody)
    {
        Exception exception = kind switch
        {
            "Forbidden" => new ForbiddenAccessException(),
            "NotFound" => new NotFoundException("missing-run", "Payroll run"),
            _ => new ConflictException("PayrollCalculationStale: authoritative sources changed.")
        };
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        bool handled = await new ProblemDetailsExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(expectedStatus);
        context.Response.Body.Position = 0;
        string response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        response.ShouldContain(expectedBody, Case.Insensitive);
    }
}
