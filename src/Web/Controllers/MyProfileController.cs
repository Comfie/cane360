using Cane360.Application.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/my-profile")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MyProfileController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetMyProfile")]
    public async Task<ActionResult<AccountProfileDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetMyProfileQuery(), cancellationToken));
    }

    [HttpPut(Name = "UpdateMyProfile")]
    public async Task<ActionResult<AccountProfileDto>> Update(UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(request, cancellationToken));
    }
}
