using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Api.Security;

public sealed class PermissionChecker(IHttpContextAccessor http, IConfiguration configuration) : IPermissionChecker
{
    public IReadOnlyList<string> AllowedOperations() => configuration.GetSection("Permissions").AsEnumerable()
        .Where(pair => !string.IsNullOrWhiteSpace(pair.Value) && http.HttpContext?.User.HasClaim("permisos", pair.Value!) == true)
        .Select(pair => pair.Key["Permissions:".Length..]).OrderBy(operation => operation).ToArray();
    public void Require(string operation)
    {
        if (http.HttpContext?.User.Identity?.IsAuthenticated != true) throw ApplicationError.Unauthorized();
        var code = configuration[$"Permissions:{operation}"];
        if (string.IsNullOrWhiteSpace(code))
            throw ApplicationError.Configuration($"Falta asociar la operación '{operation}' a un permiso existente en Permissions.");
        if (!http.HttpContext.User.HasClaim("permisos", code)) throw ApplicationError.Forbidden();
    }
}
