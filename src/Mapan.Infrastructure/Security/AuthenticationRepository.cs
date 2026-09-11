using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Security;

public sealed class AuthenticationRepository(MapanDbContext db, IHttpContextAccessor http, TimeProvider clock)
    : IAuthenticationRepository
{
    public async Task<Usuario?> FindUserAsync(string identifier, CancellationToken ct)
    {
        // Un nombre puede coincidir con el correo de otro usuario: rechazar ambigüedad.
        var matches = await db.Set<Usuario>().AsNoTracking()
            .Where(u => u.NombreUsuario == identifier || u.Correo == identifier).Take(2).ToListAsync(ct);
        return matches.Count == 1 ? matches[0] : null;
    }
    public Task<Usuario?> FindUserAsync(Guid usuarioId, CancellationToken ct) => db.Set<Usuario>()
        .AsNoTracking().SingleOrDefaultAsync(u => u.UsuarioId == usuarioId, ct);

    public async Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid usuarioId, CancellationToken ct) =>
        await (from membership in db.Set<UsuarioEmpresa>().AsNoTracking()
               join company in db.Empresas.AsNoTracking() on membership.EmpresaId equals company.EmpresaId
               where membership.UsuarioId == usuarioId && membership.Estado == "ACTIVO" && company.Estado == "ACTIVA"
               orderby company.Codigo
               select new MembershipDto(membership.UsuarioEmpresaId, company.EmpresaId, company.Codigo,
                   company.NombreLegal, membership.SucursalPredeterminadaId)).ToListAsync(ct);

    public async Task<TenantIdentity?> GetIdentityAsync(Guid usuarioId, Guid empresaId, CancellationToken ct)
    {
        var user = await FindUserAsync(usuarioId, ct);
        if (user is null || user.Estado != "ACTIVO" || user.MfaHabilitado || user.BloqueadoHasta > clock.GetUtcNow()) return null;
        var membership = (await GetMembershipsAsync(usuarioId, ct)).SingleOrDefault(m => m.EmpresaId == empresaId);
        if (membership is null) return null;
        var roles = from assignment in db.Set<UsuarioEmpresaRol>().AsNoTracking()
                    join role in db.Set<Rol>().AsNoTracking() on assignment.RolId equals role.RolId
                    where assignment.UsuarioEmpresaId == membership.UsuarioEmpresaId
                        && role.EmpresaId == empresaId && role.Estado == "ACTIVO"
                    select role;
        var roleCodes = await roles.Select(r => r.Codigo).Distinct().OrderBy(c => c).ToListAsync(ct);
        var permissionCodes = await (from role in roles
                                     join grant in db.Set<RolPermiso>() on role.RolId equals grant.RolId
                                     join permission in db.Set<Permiso>() on grant.PermisoId equals permission.PermisoId
                                     select permission.Codigo).Distinct().OrderBy(c => c).ToListAsync(ct);
        return new(usuarioId, user.NombreUsuario, membership, roleCodes, permissionCodes);
    }

    public async Task UpdatePasswordHashAsync(Guid usuarioId, string passwordHash, CancellationToken ct)
    {
        await db.Set<Usuario>().Where(u => u.UsuarioId == usuarioId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.PasswordHash, passwordHash)
                .SetProperty(u => u.FechaActualizacion, clock.GetUtcNow())
                .SetProperty(u => u.IntentosFallidos, 0), ct);
        db.Set<Evento>().Add(new Evento {
            UsuarioId = usuarioId, Origen = "API", EntidadTipo = "seguridad.usuario",
            Accion = "RESTABLECER_CLAVE", Exitoso = true, FechaEvento = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordLoginAsync(Usuario? user, bool successful, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (user is not null)
        {
            var query = db.Set<Usuario>().Where(u => u.UsuarioId == user.UsuarioId);
            if (successful)
                await query.ExecuteUpdateAsync(setters => setters.SetProperty(u => u.UltimoAcceso, clock.GetUtcNow())
                    .SetProperty(u => u.IntentosFallidos, 0), ct);
            else
                await query.ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IntentosFallidos, u => u.IntentosFallidos + 1), ct);
        }
        db.Set<Evento>().Add(new Evento {
            UsuarioId = user?.UsuarioId, CorrelationId = http.HttpContext?.TraceIdentifier,
            Origen = "API", EntidadTipo = "seguridad.usuario", Accion = successful ? "LOGIN_EXITOSO" : "LOGIN_FALLIDO",
            Exitoso = successful, FechaEvento = clock.GetUtcNow()
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
