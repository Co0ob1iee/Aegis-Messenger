using Aegis.Modules.Users.Application.Commands.SetOnlineStatus;
using Aegis.Modules.Users.Application.Commands.UpdateProfile;
using Aegis.Modules.Users.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Modules.Users.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var command = new UpdateProfileCommand(userId, request.DisplayName, request.Bio, request.AvatarUrl);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok();
    }

    [HttpPost("status")]
    public async Task<IActionResult> SetOnlineStatus([FromBody] OnlineStatus status)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var command = new SetOnlineStatusCommand(userId, status);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok();
    }
}

public record UpdateProfileRequest(string? DisplayName, string? Bio, string? AvatarUrl);
