using FluentValidation.Results;
using ApplicationValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.FarmSetup;

internal static class FarmSetupValidation
{
    public static ApplicationValidationException Failure(string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);

    public static string RequireUserId(IUser user) =>
        user.Id ?? throw new UnauthorizedAccessException();
}
