using System.Threading.RateLimiting;
using Mapan.Api.Security;
using Mapan.Application.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace Mapan.Api;

public static class ApiRegistration
{
    public static IServiceCollection AddMapanApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        var uploadLimit=configuration.GetValue("Storage:MaxBytes",20L*1024*1024);
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options=>options.MultipartBodyLengthLimit=uploadLimit+1024*1024);
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options=>options.Limits.MaxRequestBodySize=uploadLimit+1024*1024);
        services.AddOpenApi();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddScoped<CurrentContext>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentContext>());
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentContext>());
        services.AddRateLimiter(options => {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions {
                    PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Any(o => o.Contains('*') || !Uri.TryCreate(o, UriKind.Absolute, out _)))
            throw new InvalidOperationException("Cors:AllowedOrigins requiere orígenes explícitos válidos, sin wildcard.");
        services.AddCors(options => options.AddPolicy("Frontend", policy => {
            if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }));
        return services;
    }
}
