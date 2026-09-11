using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Api.Security;

public sealed class CurrentContext(IHttpContextAccessor accessor) : ICurrentUser, ICurrentTenant
{
    public Guid UsuarioId => RequiredId("usuario_id");
    public Guid EmpresaId => RequiredId("empresa_id");
    public Guid UsuarioEmpresaId => RequiredId("usuario_empresa_id");
    public string NombreUsuario => accessor.HttpContext?.User.FindFirst("nombre_usuario")?.Value
        ?? throw ApplicationError.Unauthorized();
    private Guid RequiredId(string claim) => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        && accessor.HttpContext.User.FindFirst("token_use")?.Value == "tenant"
        && Guid.TryParse(accessor.HttpContext.User.FindFirst(claim)?.Value, out var id) && id != Guid.Empty
        ? id : throw ApplicationError.Unauthorized();
}
