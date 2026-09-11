using Mapan.Application.Common;

namespace Mapan.Application.Security;
public sealed record SessionDto(TenantIdentity Identity,IReadOnlyList<string> AllowedOperations);
public sealed class SessionService(IAuthenticationRepository repository,ICurrentUser user,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public async Task<SessionDto> GetAsync(CancellationToken ct)=>new(
        await repository.GetIdentityAsync(user.UsuarioId,tenant.EmpresaId,ct)??throw ApplicationError.Unauthorized(),permissions.AllowedOperations());
}
