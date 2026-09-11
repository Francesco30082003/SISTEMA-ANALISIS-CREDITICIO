using Mapan.Application.Cartera;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

public sealed class CarteraMoraRepository(MapanDbContext db) : ICarteraMoraRepository
{
    // Un jefe de agencia (usuario_empresa.sucursal_predeterminada_id no nulo) solo ve la mora de SU
    // sucursal; gerencia/auditoría/admin (sin sucursal fija) siguen viendo toda la empresa — la
    // misma regla se repite igual en AnalistaDesempenoRepository y OperationsRepository("prestamos").
    public async Task<IReadOnlyList<MoraClienteDto>> ListAsync(Guid empresaId, Guid member, CancellationToken ct)
    {
        var sucursalUsuario = await db.Set<UsuarioEmpresa>().AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.UsuarioEmpresaId == member)
            .Select(x => x.SucursalPredeterminadaId).SingleOrDefaultAsync(ct);

        var ultimosCortes = db.Set<DesempenoCredito>().Where(d => d.EmpresaId == empresaId)
            .GroupBy(d => d.PrestamoId).Select(g => new { PrestamoId = g.Key, FechaCorte = g.Max(x => x.FechaCorte) });

        var query =
            from d in db.Set<DesempenoCredito>().AsNoTracking()
            join u in ultimosCortes on new { d.PrestamoId, d.FechaCorte } equals new { u.PrestamoId, u.FechaCorte }
            join p in db.Set<Prestamo>().AsNoTracking() on new { d.EmpresaId, d.PrestamoId } equals new { p.EmpresaId, p.PrestamoId }
            join s in db.Set<SolicitudCredito>().AsNoTracking() on new { p.EmpresaId, p.SolicitudCreditoId } equals new { s.EmpresaId, s.SolicitudCreditoId }
            join c in db.Set<Cliente>().AsNoTracking() on new { s.EmpresaId, s.ClienteId } equals new { c.EmpresaId, c.ClienteId }
            join suc in db.Set<Sucursal>().AsNoTracking() on new { s.EmpresaId, s.SucursalId } equals new { suc.EmpresaId, suc.SucursalId }
            join ue in db.Set<UsuarioEmpresa>().AsNoTracking() on new { s.EmpresaId, UsuarioEmpresaId = s.CreadoPorUsuarioEmpresaId } equals new { ue.EmpresaId, ue.UsuarioEmpresaId }
            join us in db.Set<Usuario>().AsNoTracking() on ue.UsuarioId equals us.UsuarioId
            where d.EmpresaId == empresaId && d.DiasMora > 0 && (sucursalUsuario == null || s.SucursalId == sucursalUsuario)
            select new { d, p, c, suc, us };

        var rows = await query.ToListAsync(ct);
        return rows.Select(r => new MoraClienteDto(
                r.p.PrestamoId, r.p.NumeroPrestamo, r.c.ClienteId,
                r.c.TipoPersona == "JURIDICA" ? r.c.RazonSocial ?? "" : $"{r.c.Nombres} {r.c.Apellidos}".Trim(),
                r.c.Telefono, r.c.Correo, r.suc.SucursalId, r.suc.Nombre, $"{r.us.Nombres} {r.us.Apellidos}".Trim(),
                r.d.DiasMora, Banda(r.d.DiasMora), r.d.SaldoCapital, r.d.CuotaExigible, r.d.FechaCorte))
            .OrderByDescending(x => x.DiasMora).ToList();
    }

    private static string Banda(int dias) => dias switch { >= 90 => "90+", >= 60 => "60-89", >= 30 => "30-59", >= 15 => "15-29", _ => "1-14" };

    public async Task<IReadOnlyList<string>> DestinatariosAlertaAsync(Guid empresaId, CancellationToken ct) =>
        await (from ur in db.Set<UsuarioEmpresaRol>().AsNoTracking()
               join ue in db.Set<UsuarioEmpresa>().AsNoTracking() on ur.UsuarioEmpresaId equals ue.UsuarioEmpresaId
               join r in db.Set<Rol>().AsNoTracking() on ur.RolId equals r.RolId
               join rp in db.Set<RolPermiso>().AsNoTracking() on r.RolId equals rp.RolId
               join perm in db.Set<Permiso>().AsNoTracking() on rp.PermisoId equals perm.PermisoId
               join u in db.Set<Usuario>().AsNoTracking() on ue.UsuarioId equals u.UsuarioId
               where ue.EmpresaId == empresaId && r.EmpresaId == empresaId && r.Estado == "ACTIVO" && ue.Estado == "ACTIVO" && perm.Codigo == "Mora:Notificar"
               select u.Correo).Distinct().ToListAsync(ct);
}
