using Mapan.Application.Analistas;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

// Reporte agregado, no un endpoint transaccional caliente: se resuelve trayendo las filas relevantes
// de la empresa y agrupando en memoria — a la escala de una cooperativa (cientos/miles de solicitudes,
// no millones) es simple y suficientemente rápido, y evita GroupBy+First que EF Core no siempre traduce.
public sealed class AnalistaDesempenoRepository(MapanDbContext db) : IAnalistaDesempenoRepository
{
    // Mismo criterio que CarteraMoraRepository/OperationsRepository("prestamos"): un jefe de agencia
    // (sucursal_predeterminada_id no nulo) solo ve el desempeño de SU sucursal; gerencia/auditoría/
    // admin (sin sucursal fija) ven toda la empresa.
    public async Task<IReadOnlyList<AnalistaDesempenoDto>> ListAsync(Guid empresaId, Guid member, CancellationToken ct)
    {
        var sucursalUsuario = await db.Set<UsuarioEmpresa>().AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.UsuarioEmpresaId == member)
            .Select(x => x.SucursalPredeterminadaId).SingleOrDefaultAsync(ct);

        var solicitudes = await db.Set<SolicitudCredito>().AsNoTracking()
            .Where(s => s.EmpresaId == empresaId && (sucursalUsuario == null || s.SucursalId == sucursalUsuario))
            .Select(s => new { s.SolicitudCreditoId, s.CreadoPorUsuarioEmpresaId, s.SucursalId, s.Estado })
            .ToListAsync(ct);
        if (solicitudes.Count == 0) return [];

        var usuarioEmpresaIds = solicitudes.Select(s => s.CreadoPorUsuarioEmpresaId).Distinct().ToList();
        var sucursalIds = solicitudes.Select(s => s.SucursalId).Distinct().ToList();

        var nombresPorUsuarioEmpresa = await (
            from ue in db.Set<UsuarioEmpresa>().AsNoTracking()
            join u in db.Set<Usuario>().AsNoTracking() on ue.UsuarioId equals u.UsuarioId
            where ue.EmpresaId == empresaId && usuarioEmpresaIds.Contains(ue.UsuarioEmpresaId)
            select new { ue.UsuarioEmpresaId, Nombre = u.Nombres + " " + u.Apellidos }
        ).ToDictionaryAsync(x => x.UsuarioEmpresaId, x => x.Nombre, ct);

        var nombresPorSucursal = await db.Set<Sucursal>().AsNoTracking()
            .Where(s => s.EmpresaId == empresaId && sucursalIds.Contains(s.SucursalId))
            .ToDictionaryAsync(s => s.SucursalId, s => s.Nombre, ct);

        var solicitudIds = solicitudes.Select(s => s.SolicitudCreditoId).ToList();
        var prestamos = await db.Set<Prestamo>().AsNoTracking()
            .Where(p => p.EmpresaId == empresaId && solicitudIds.Contains(p.SolicitudCreditoId))
            .Select(p => new { p.PrestamoId, p.SolicitudCreditoId, p.MontoDesembolsado })
            .ToListAsync(ct);
        var prestamoIds = prestamos.Select(p => p.PrestamoId).ToList();

        var ultimasFechas = db.Set<DesempenoCredito>().Where(d => d.EmpresaId == empresaId && prestamoIds.Contains(d.PrestamoId))
            .GroupBy(d => d.PrestamoId).Select(g => new { PrestamoId = g.Key, FechaCorte = g.Max(x => x.FechaCorte) });
        var ultimosCortes = prestamoIds.Count == 0 ? [] : await (
            from d in db.Set<DesempenoCredito>().AsNoTracking()
            join u in ultimasFechas on new { d.PrestamoId, d.FechaCorte } equals new { u.PrestamoId, u.FechaCorte }
            where d.EmpresaId == empresaId
            select d).ToListAsync(ct);

        var moraPorPrestamo = ultimosCortes.ToDictionary(d => d.PrestamoId, d => d.DiasMora);
        var saldoPorPrestamo = ultimosCortes.ToDictionary(d => d.PrestamoId, d => d.SaldoCapital);
        var montoPorSolicitud = prestamos.ToDictionary(p => p.SolicitudCreditoId, p => p.MontoDesembolsado);
        var prestamoIdPorSolicitud = prestamos.ToDictionary(p => p.SolicitudCreditoId, p => p.PrestamoId);

        var resultado = new List<AnalistaDesempenoDto>();
        foreach (var g in solicitudes.GroupBy(s => (s.CreadoPorUsuarioEmpresaId, s.SucursalId)))
        {
            var total = g.Count();
            var aprobadas = g.Count(s => s.Estado == "APROBADA");
            var rechazadas = g.Count(s => s.Estado == "RECHAZADA");
            var enProceso = total - aprobadas - rechazadas;
            var montoColocado = g.Sum(s => montoPorSolicitud.GetValueOrDefault(s.SolicitudCreditoId, 0m));
            var decididas = aprobadas + rechazadas;
            decimal? tasaAprobacion = decididas > 0 ? Math.Round(100m * aprobadas / decididas, 1) : null;
            var prestamosDelGrupo = g.Where(s => prestamoIdPorSolicitud.ContainsKey(s.SolicitudCreditoId))
                .Select(s => prestamoIdPorSolicitud[s.SolicitudCreditoId]).ToList();
            var enMora = prestamosDelGrupo.Count(pid => moraPorPrestamo.GetValueOrDefault(pid, 0) > 0);
            var saldoEnMora = prestamosDelGrupo.Where(pid => moraPorPrestamo.GetValueOrDefault(pid, 0) > 0)
                .Sum(pid => saldoPorPrestamo.GetValueOrDefault(pid, 0m));
            resultado.Add(new AnalistaDesempenoDto(
                g.Key.CreadoPorUsuarioEmpresaId, nombresPorUsuarioEmpresa.GetValueOrDefault(g.Key.CreadoPorUsuarioEmpresaId, "—"),
                g.Key.SucursalId, nombresPorSucursal.GetValueOrDefault(g.Key.SucursalId, "—"),
                total, aprobadas, rechazadas, enProceso, montoColocado, tasaAprobacion, enMora, saldoEnMora));
        }
        return resultado.OrderByDescending(x => x.MontoColocado).ToList();
    }
}
