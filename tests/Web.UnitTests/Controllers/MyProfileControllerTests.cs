using System.Text.Json;
using Cane360.Application.Account;
using Cane360.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class MyProfileControllerTests
{
    [Test]
    public void ProfileRequiresAuthenticationAndPreventsCaching()
    {
        typeof(MyProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        typeof(MyProfileController).GetCustomAttributes(typeof(ResponseCacheAttribute), true)
            .Cast<ResponseCacheAttribute>().Single().NoStore.ShouldBeTrue();
    }

    [Test]
    public void UpdatePayloadCannotSelectAnotherUserOrAssignARole()
    {
        var request = JsonSerializer.Deserialize<UpdateMyProfileCommand>(
            """{"displayName":"Synthetic","phoneNumber":null,"userId":"other-user","role":"Grower","email":"other@example.invalid"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        request.ShouldBe(new UpdateMyProfileCommand("Synthetic", null));
        typeof(UpdateMyProfileCommand).GetProperties().Select(property => property.Name)
            .ShouldBe(["DisplayName", "PhoneNumber"]);
    }
}
