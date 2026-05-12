using ECommerceAPI.Application.DTOs.Auth;
using ECommerceAPI.Application.Features.Auth.Commands.Login;
using ECommerceAPI.Application.Features.Auth.Commands.RefreshToken;
using ECommerceAPI.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Register a new customer account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.ConfirmPassword);

        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>Login with email and password. Returns JWT access + refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>Use refresh token to get a new access token without re-login.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Data);
    }

    /// <summary>Test endpoint — any authenticated user can access.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new { userId, email, role, message = "Token is valid." });
    }

    /// <summary>Admin-only test endpoint.</summary>
    [HttpGet("admin-test")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public IActionResult AdminTest()
        => Ok(new { message = "You are an admin!" });
}
