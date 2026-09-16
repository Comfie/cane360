using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Web.Infrastructure;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Web.UnitTests.Infrastructure;

public class ProblemDetailsExceptionHandlerTests
{
    [TestCase(false, 500)]
    [TestCase(true, 409)]
    public async Task ReleaseFailuresExposeSupportReferenceWithoutInternalExceptionDetails(bool concurrency, int status)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "AUTOTEST-P8A-support" };
        context.Response.Body = new MemoryStream();
        Exception exception = concurrency
            ? new DbUpdateConcurrencyException("PRIVATE_DATABASE_DETAIL")
            : new InvalidOperationException("PRIVATE_DATABASE_DETAIL");

        bool handled = await new ProblemDetailsExceptionHandler().TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(status);
        context.Response.Body.Position = 0;
        string response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        response.ShouldContain("AUTOTEST-P8A-support");
        response.ShouldNotContain("PRIVATE_DATABASE_DETAIL");
    }

    [Test]
    public async Task NotFoundDoesNotEchoForeignResourceIdentifiers()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await new ProblemDetailsExceptionHandler().TryHandleAsync(context,
            new NotFoundException("foreign-record-identifier", "Payroll"), CancellationToken.None);
        context.Response.Body.Position = 0;
        string response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        response.ShouldNotContain("foreign-record-identifier");
        context.Response.StatusCode.ShouldBe(404);
    }

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
