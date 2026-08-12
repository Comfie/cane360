using System.Security.Claims;
using Cane360.Infrastructure.Identity;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Auth;
using Cane360.Web.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class UsersControllerTests
{
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManager = null!;
    private UsersController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _userManager = IdentityManagerMocks.CreateUserManager();
        _signInManager = IdentityManagerMocks.CreateSignInManager(_userManager.Object);
        _controller = new UsersController(_userManager.Object, _signInManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Test]
    public async Task RegisterReturnsBadRequestWhenIdentityRejectsUser()
    {
        _userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password is too short."
            }));

        var result = await _controller.Register(
            new RegisterRequest("user@example.com", "weak"));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ValidationProblemDetails>();
        problem.Errors["PasswordTooShort"].ShouldBe(["Password is too short."]);
    }

    [Test]
    public async Task RegisterReturnsOkWhenIdentityCreatesUser()
    {
        _userManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "StrongPassword1!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(
            new RegisterRequest("user@example.com", "StrongPassword1!"));

        result.ShouldBeOfType<OkResult>();
    }

    [Test]
    public async Task LoginReturnsUnauthorizedForInvalidCredentials()
    {
        _signInManager
            .Setup(x => x.PasswordSignInAsync("user@example.com", "wrong", true, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _controller.Login(
            useCookies: true,
            useSessionCookies: null,
            new LoginRequest("user@example.com", "wrong"));

        result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task LogoutSignsOutAndReturnsOk()
    {
        var result = await _controller.Logout();

        _signInManager.Verify(x => x.SignOutAsync(), Times.Once);
        result.ShouldBeOfType<OkResult>();
    }

    [Test]
    public async Task InfoReturnsCurrentUserDetails()
    {
        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com"
        };
        _controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));
        _userManager
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _userManager
            .Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);

        var result = await _controller.Info();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe(new UserInfoResponse("user@example.com", true));
    }
}
