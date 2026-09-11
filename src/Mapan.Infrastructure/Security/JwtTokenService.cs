using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Microsoft.IdentityModel.Tokens;

namespace Mapan.Infrastructure.Security;

public sealed record JwtSettings(string Issuer, string Audience, string SigningKey, int LifetimeMinutes = 15)
{
    public TokenValidationParameters ValidationParameters => new() {
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = true, ValidAudience = Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
        ValidateLifetime = true, RequireExpirationTime = true, RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(15), ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = "nombre_usuario", RoleClaimType = "roles"
    };
}

public sealed class JwtTokenService(JwtSettings settings, TimeProvider clock) : ITokenService
{
    public IssuedToken IssueTenantToken(TenantIdentity identity)
    {
        var claims = new List<Claim> {
            new("sub", identity.UsuarioId.ToString()), new("usuario_id", identity.UsuarioId.ToString()),
            new("usuario_empresa_id", identity.Membership.UsuarioEmpresaId.ToString()),
            new("empresa_id", identity.Membership.EmpresaId.ToString()), new("nombre_usuario", identity.NombreUsuario),
            new("token_use", "tenant")
        };
        claims.AddRange(identity.Roles.Select(r => new Claim("roles", r)));
        claims.AddRange(identity.Permisos.Select(p => new Claim("permisos", p)));
        return Issue(claims, TimeSpan.FromMinutes(settings.LifetimeMinutes));
    }
    public IssuedToken IssueSelectionToken(Guid usuarioId) => Issue([
        new("sub", usuarioId.ToString()), new("token_use", "select_empresa")], TimeSpan.FromMinutes(5));

    public Guid ValidateSelectionToken(string token)
    {
        try {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(token, settings.ValidationParameters, out _);
            if (principal.FindFirst("token_use")?.Value != "select_empresa"
                || !Guid.TryParse(principal.FindFirst("sub")?.Value, out var id) || id == Guid.Empty)
                throw ApplicationError.Unauthorized();
            return id;
        }
        catch (SecurityTokenException) { throw ApplicationError.Unauthorized(); }
        catch (ArgumentException) { throw ApplicationError.Unauthorized(); }
    }

    private IssuedToken Issue(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var now = clock.GetUtcNow();
        var expires = now.Add(lifetime);
        var all = claims.Concat([new Claim("jti", Guid.NewGuid().ToString()),
            new Claim("iat", now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)]);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, all,
            now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(settings.ValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
