using Mapan.Domain.Entities;

namespace Mapan.Application.Security;

public interface ICurrentUser
{
    Guid UsuarioId { get; }
    string NombreUsuario { get; }
}

public interface ICurrentTenant
{
    Guid EmpresaId { get; }
    Guid UsuarioEmpresaId { get; }
}

public sealed record MembershipDto(Guid UsuarioEmpresaId, Guid EmpresaId, string Codigo,
    string NombreLegal, Guid? SucursalPredeterminadaId);
public sealed record TenantIdentity(Guid UsuarioId, string NombreUsuario, MembershipDto Membership,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permisos);
public sealed record AuthResponse(string? AccessToken, string? SelectionToken,
    DateTimeOffset ExpiresAt, IReadOnlyList<MembershipDto> Empresas);
public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

public interface IAuthenticationRepository
{
    Task<Usuario?> FindUserAsync(string identifier, CancellationToken ct);
    Task<Usuario?> FindUserAsync(Guid usuarioId, CancellationToken ct);
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid usuarioId, CancellationToken ct);
    Task<TenantIdentity?> GetIdentityAsync(Guid usuarioId, Guid empresaId, CancellationToken ct);
    Task RecordLoginAsync(Usuario? user, bool successful, CancellationToken ct);
    Task UpdatePasswordHashAsync(Guid usuarioId, string passwordHash, CancellationToken ct);
}

public interface IPasswordService
{
    bool Verify(Usuario user, string password);
    string Hash(Usuario user, string password);
}

public interface ITokenService
{
    IssuedToken IssueTenantToken(TenantIdentity identity);
    IssuedToken IssueSelectionToken(Guid usuarioId);
    Guid ValidateSelectionToken(string token);
}
