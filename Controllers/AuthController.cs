using Concertable.Auth.Data;
using Concertable.Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Auth.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var redirectUrl = await _authService.LoginAsync(request.Email, request.Password, request.ReturnUrl, HttpContext);
        if (redirectUrl is null)
            return Unauthorized("Invalid credentials");

        return Ok(new { redirectUrl });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var success = await _authService.RegisterAsync(request.Email, request.Password, request.Role);
        if (!success)
            return Conflict("Email already in use");

        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var redirectUrl = await _authService.LogoutAsync(request.LogoutId, HttpContext);
        return Ok(new { redirectUrl });
    }
}

public record LoginRequest(string Email, string Password, string ReturnUrl);
public record RegisterRequest(string Email, string Password, Role Role);
public record LogoutRequest(string LogoutId);
