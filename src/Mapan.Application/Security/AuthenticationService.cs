using Mapan.Application.Common;
using Mapan.Domain.Entities;

namespace Mapan.Application.Security;

public sealed class AuthenticationService(IAuthenticationRepository repository,
    IPasswordService passwords, ITokenService tokens, TimeProvider clock)
{
    public async Task<AuthResponse> LoginAsync(string identifier, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 200 || string.IsNullOrEmpty(password) || password.Length > 1024)
            throw ApplicationError.Unauthorized();
        var user = await repository.FindUserAsync(identifier.Trim(), ct);
        var valid = user is not null && passwords.Verify(user, password) && IsActive(user);
        if (!valid)
        {
            await repository.RecordLoginAsync(user, false, ct);
            throw ApplicationError.Unauthorized();
        }
        if (user!.MfaHabilitado)
            throw ApplicationError.Configuration("El usuario requiere MFA; el proveedor MFA aún no está configurado.");
        var memberships = await repository.GetMembershipsAsync(user.UsuarioId, ct);
        if (memberships.Count == 0)
        {
            await repository.RecordLoginAsync(user, false, ct);
            throw ApplicationError.Forbidden();
        }
        await repository.RecordLoginAsync(user, true, ct);
        if (memberships.Count == 1)
            return await IssueAsync(user.UsuarioId, memberships[0].EmpresaId, ct);
        var token = tokens.IssueSelectionToken(user.UsuarioId);
        return new(null, token.Value, token.ExpiresAt, memberships);
    }

    public async Task<AuthResponse> SelectEmpresaAsync(string selectionToken, Guid empresaId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(selectionToken) || selectionToken.Length > 16384)
            throw ApplicationError.Unauthorized();
        var usuarioId = tokens.ValidateSelectionToken(selectionToken);
        var user = await repository.FindUserAsync(usuarioId, ct);
        if (user is null || !IsActive(user) || user.MfaHabilitado) throw ApplicationError.Unauthorized();
        return await IssueAsync(usuarioId, empresaId, ct);
    }

    private bool IsActive(Usuario user) => user.Estado == "ACTIVO"
        && (user.BloqueadoHasta is null || user.BloqueadoHasta <= clock.GetUtcNow());

    private async Task<AuthResponse> IssueAsync(Guid usuarioId, Guid empresaId, CancellationToken ct)
    {
        var identity = await repository.GetIdentityAsync(usuarioId, empresaId, ct)
            ?? throw ApplicationError.Forbidden();
        var token = tokens.IssueTenantToken(identity);
        return new(token.Value, null, token.ExpiresAt, [identity.Membership]);
    }
}
