using System.Diagnostics;
using Cane360.Web.Infrastructure;
using Cane360.Infrastructure.Identity;
using Cane360.Web.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cane360.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(ApiRateLimitOptions.UsersPolicy)]
public sealed class UsersController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    private static readonly TimeSpan MinimumRegistrationDuration = TimeSpan.FromMilliseconds(300);

    [AllowAnonymous]
    [HttpPost("register")]
    [EndpointSummary("Register")]
    [EndpointDescription("Creates a new user account.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var timer = Stopwatch.StartNew();
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);

        var visibleErrors = result.Errors
            .Where(error => error.Code is not ("DuplicateUserName" or "DuplicateEmail"))
            .ToArray();

        if (result.Succeeded || (visibleErrors.Length == 0 && result.Errors.Any()))
        {
            var remaining = MinimumRegistrationDuration - timer.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, HttpContext.RequestAborted);
            }

            return Ok();
        }

        var errors = visibleErrors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return BadRequest(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Registration failed."
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EndpointSummary("Log in")]
    [EndpointDescription("Authenticates a user with the Identity application cookie.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromQuery] bool useCookies,
        [FromQuery] bool? useSessionCookies,
        LoginRequest request)
    {
        var isPersistent = useCookies && useSessionCookies != true;
        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent,
            lockoutOnFailure: true);

        return result.Succeeded ? Ok() : Unauthorized();
    }

    [Authorize]
    [HttpPost("logout")]
    [EndpointSummary("Log out")]
    [EndpointDescription("Ends the current Identity cookie session.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok();
    }

    [Authorize]
    [HttpGet("manage/info")]
    [EndpointSummary("Get account info")]
    [EndpointDescription("Returns the current user's account information.")]
    [ProducesResponseType<UserInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Info()
    {
        var user = await userManager.GetUserAsync(User);

        if (user?.Email is null)
        {
            return Unauthorized();
        }

        var isEmailConfirmed = await userManager.IsEmailConfirmedAsync(user);
        return Ok(new UserInfoResponse(user.Email, isEmailConfirmed));
    }
}
