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
}
