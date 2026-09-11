using System.Security.Claims;
using System.Text;
using Mapan.Application.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mapan.Infrastructure.Security;

public static class SecurityRegistration
{
    public static IServiceCollection AddMapanSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Configure Jwt:SigningKey con un secreto aleatorio de al menos 32 bytes mediante User Secrets o entorno.");
        var settings = new JwtSettings(configuration["Jwt:Issuer"] ?? "Mapan",
            configuration["Jwt:Audience"] ?? "Mapan.Api", key, configuration.GetValue("Jwt:LifetimeMinutes", 240));
        if (settings.LifetimeMinutes is < 1 or > 480)
            throw new InvalidOperationException("Jwt:LifetimeMinutes debe estar entre 1 y 480.");
        services.AddSingleton(settings);
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<ISecurityAdministrationRepository, SecurityAdministrationRepository>();
        services.AddHttpContextAccessor();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = settings.ValidationParameters;
            options.Events = new JwtBearerEvents {
                OnTokenValidated = async context => {
                    var principal = context.Principal!;
                    if (principal.FindFirst("token_use")?.Value != "tenant"
                        || !Guid.TryParse(principal.FindFirst("usuario_id")?.Value, out var user)
                        || !Guid.TryParse(principal.FindFirst("empresa_id")?.Value, out var company)
                        || !Guid.TryParse(principal.FindFirst("usuario_empresa_id")?.Value, out var membership)) {
                        context.Fail("Token tenant requerido."); return;
                    }
                    var repository = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationRepository>();
                    var current = await repository.GetIdentityAsync(user, company, context.HttpContext.RequestAborted);
                    if (current is null || current.Membership.UsuarioEmpresaId != membership) {
                        context.Fail("Membresía no vigente."); return;
                    }
                    // Revocaciones y cambios de permisos se aplican en cada request.
                    var identity = (ClaimsIdentity)principal.Identity!;
                    foreach (var claim in identity.FindAll("permisos").Concat(identity.FindAll("roles")).ToArray())
                        identity.RemoveClaim(claim);
                    identity.AddClaims(current.Permisos.Select(p => new Claim("permisos", p)));
                    identity.AddClaims(current.Roles.Select(r => new Claim("roles", r)));
                }
            };
        });
        services.AddAuthorization(options => {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("token_use", "tenant").Build();
            options.DefaultPolicy = options.FallbackPolicy;
        });
        return services;
    }
}
