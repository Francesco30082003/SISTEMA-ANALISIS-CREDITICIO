using Mapan.Application.Cartera;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

// Análisis de cosechas (vintage analysis): agrupa préstamos por mes de desembolso ("cosecha") y sigue
// su deterioro mes a mes (MOB = meses desde el desembolso). No introduce datos nuevos: reutiliza
// exactamente lo mismo que ya alimenta el panel de mora (DesempenoCredito, un corte mensual por
// préstamo) — igual que AnalistaDesempenoRepository, se agrupa en memoria a la escala de una
// cooperativa para evitar GroupBy+First que EF Core no siempre traduce bien.
public sealed class CosechaRepository(MapanDbContext db) : ICosechaRepository
{
    public async Task<IReadOnlyList<CosechaDto>> ListAsync(Guid empresaId,Guid? productoCreditoId,Guid? sucursalId,CancellationToken ct)
    {
        var prestamos = await
            (from p in db.Set<Prestamo>().AsNoTracking()
             join s in db.Set<SolicitudCredito>().AsNoTracking() on new{p.EmpresaId,p.SolicitudCreditoId} equals new{s.EmpresaId,s.SolicitudCreditoId}
             where p.EmpresaId==empresaId
                && (productoCreditoId==null || s.ProductoCreditoId==productoCreditoId)
                && (sucursalId==null || s.SucursalId==sucursalId)
             select new { p.PrestamoId,p.FechaDesembolso,p.MontoDesembolsado }
            ).ToListAsync(ct);
        if(prestamos.Count==0)return [];

        var prestamoIds = prestamos.Select(p=>p.PrestamoId).ToList();
        var desempenos = await db.Set<DesempenoCredito>().AsNoTracking().Where(d=>d.EmpresaId==empresaId&&prestamoIds.Contains(d.PrestamoId))
            .Select(d=>new{d.PrestamoId,d.FechaCorte,d.SaldoCapital,d.Mora30}).ToListAsync(ct);

        var desembolsoPorPrestamo = prestamos.ToDictionary(p=>p.PrestamoId,p=>p.FechaDesembolso);
        // Un préstamo no debería tener dos cortes en el mismo MOB, pero si ocurriera se toma el más reciente.
        var cortesPorMob = desempenos
            .Select(d=>new{d,Mob=MesesEntre(desembolsoPorPrestamo[d.PrestamoId],d.FechaCorte)})
            .Where(x=>x.Mob>=0)
            .GroupBy(x=>(x.d.PrestamoId,x.Mob))
            .Select(g=>g.OrderByDescending(x=>x.d.FechaCorte).First())
            .ToList();

        var resultado = new List<CosechaDto>();
        foreach(var grupo in prestamos.GroupBy(p=>Cosecha(p.FechaDesembolso)).OrderBy(g=>g.Key))
        {
            var idsCosecha = grupo.Select(p=>p.PrestamoId).ToHashSet();
            var puntos = new List<CosechaPuntoDto>();
            foreach(var mobGrupo in cortesPorMob.Where(x=>idsCosecha.Contains(x.d.PrestamoId)).GroupBy(x=>x.Mob).OrderBy(g=>g.Key))
            {
                var activos = mobGrupo.Count();
                var enMora = mobGrupo.Count(x=>x.d.Mora30==true);
                var saldoActivo = mobGrupo.Sum(x=>x.d.SaldoCapital);
                var saldoEnMora = mobGrupo.Where(x=>x.d.Mora30==true).Sum(x=>x.d.SaldoCapital);
                decimal? tasa = activos>0?Math.Round(100m*enMora/activos,1):null;
                puntos.Add(new(mobGrupo.Key,activos,enMora,saldoActivo,saldoEnMora,tasa));
            }
            resultado.Add(new(grupo.Key,grupo.Count(),grupo.Sum(p=>p.MontoDesembolsado),puntos));
        }
        return resultado;
    }

    private static string Cosecha(DateOnly fecha)=>$"{fecha.Year:D4}-{fecha.Month:D2}";
    private static int MesesEntre(DateOnly desde,DateOnly hasta)=>(hasta.Year-desde.Year)*12+(hasta.Month-desde.Month);
}
