using System.Security.Claims;
using NexoStock.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexoStock.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized(new { message = "Credenciales invalidas." });
        }

        return Ok(response);
    }

    [HttpPost("users")]
    [Authorize(Policy = ApplicationPolicies.UsersCreate)]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.CreateUserAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var response = new { message = "No fue posible crear el usuario.", errors = result.Errors };
            return result.IsConflict ? Conflict(response) : BadRequest(response);
        }

        return StatusCode(StatusCodes.Status201Created, result.User);
    }

    [HttpGet("me")]
    [Authorize(Policy = ApplicationPolicies.ProfileRead)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await authService.GetCurrentUserAsync(userId, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpGet("users")]
    [Authorize(Policy = ApplicationPolicies.UsersRead)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> Users(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetUsersAsync(cancellationToken));
    }
}
