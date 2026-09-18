using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Infrastructure;

public static class DateValidationExtensions
{
    public static BadRequestObjectResult DateValidationError(this ControllerBase controller,
        string propertyName, bool multipleDates = false) => controller.BadRequest(
        new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [propertyName] = [multipleDates ? "Dates must use yyyy-MM-dd." : "Date must use yyyy-MM-dd."]
        }));
}
