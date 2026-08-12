using System.ComponentModel.DataAnnotations;
using Cane360.Web.Models.Auth;

namespace Cane360.Web.UnitTests.Models;

public class AuthRequestTests
{
    [TestCase(typeof(LoginRequest))]
    [TestCase(typeof(RegisterRequest))]
    public void ValidationMetadataIsDefinedOnPrimaryConstructorParameters(Type requestType)
    {
        var constructor = requestType.GetConstructors().ShouldHaveSingleItem();
        var parameters = constructor.GetParameters();

        parameters.ShouldAllBe(parameter =>
            parameter.GetCustomAttributes(typeof(RequiredAttribute), inherit: true).Length == 1);
        parameters.Single(parameter => parameter.Name == "Email")
            .GetCustomAttributes(typeof(EmailAddressAttribute), inherit: true)
            .ShouldHaveSingleItem();
        requestType.GetProperties()
            .SelectMany(property => property.GetCustomAttributes(typeof(ValidationAttribute), inherit: true))
            .ShouldBeEmpty();
    }
}
