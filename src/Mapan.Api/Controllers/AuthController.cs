using System.ComponentModel.DataAnnotations;
using Mapan.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mapan.Api.Controllers;

public sealed record LoginRequest([Required, MaxLength(200)] string Usuario,
    [Required, MaxLength(1024)] string Password);
public sealed record SelectEmpresaRequest([Required, MaxLength(16384)] string SelectionToken, Guid EmpresaId);

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(AuthenticationService service, SessionService session, PasswordResetService passwordReset) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await service.LoginAsync(request.Usuario, request.Password, ct));

    [HttpPost("select-empresa"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> SelectEmpresa(SelectEmpresaRequest request, CancellationToken ct) =>
        Ok(await service.SelectEmpresaAsync(request.SelectionToken, request.EmpresaId, ct));

    [HttpPost("olvide-clave"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInput input, CancellationToken ct)
    {await passwordReset.RequestAsync(input,ct);return Ok();}

    [HttpPost("restablecer-clave"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> ResetPassword(ResetPasswordInput input, CancellationToken ct)
    {await passwordReset.ResetAsync(input,ct);return Ok();}

    [HttpGet("me")]
    public async Task<ActionResult<SessionDto>> Me(CancellationToken ct) => Ok(await session.GetAsync(ct));
}
