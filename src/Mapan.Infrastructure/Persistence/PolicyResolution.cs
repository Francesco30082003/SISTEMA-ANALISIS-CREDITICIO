using Mapan.Application.Common;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence;

// Resuelve la única política vigente aplicable a un producto (o a la empresa si el producto no tiene
// una específica), compartido por CreditAnalysisRepository (análisis financiero) y PreevaluacionRepository
// (preevaluación) — ambos necesitan exactamente la misma regla de "cuál política manda".
public static class PolicyResolution
{
    public static async Task<(PoliticaCredito Policy, PoliticaVersion Version)> ResolveVigenteAsync(
        MapanDbContext db,Guid tenant,Guid productoId,DateTimeOffset now,CancellationToken ct)
    {
        var candidates=await (from p in db.Set<PoliticaCredito>() join v in db.Set<PoliticaVersion>() on p.PoliticaCreditoId equals v.PoliticaCreditoId
            where p.EmpresaId==tenant&&v.EmpresaId==tenant&&p.Estado=="ACTIVA"&&v.Estado=="VIGENTE"&&v.VigenteDesde<=now&&(v.VigenteHasta==null||v.VigenteHasta>=now)&&(p.ProductoCreditoId==null||p.ProductoCreditoId==productoId)
            select new { Policy = p, Version = v }).ToListAsync(ct);
        if(candidates.Any(x=>x.Policy.ProductoCreditoId==productoId))candidates=candidates.Where(x=>x.Policy.ProductoCreditoId==productoId).ToList();
        if(candidates.Select(x=>x.Policy.PoliticaCreditoId).Distinct().Count()!=1)
            throw new ApplicationError(422,"POLICY_VALIDATION","Configura una única política vigente aplicable al producto o a la empresa.");
        var chosen=candidates.OrderByDescending(x=>x.Version.VigenteDesde).ThenByDescending(x=>x.Version.NumeroVersion).First();
        var version=(await db.Set<PoliticaVersion>().FromSqlInterpolated($"SELECT * FROM politica.politica_version WHERE empresa_id={tenant} AND politica_version_id={chosen.Version.PoliticaVersionId} FOR SHARE").ToListAsync(ct)).Single();
        return (chosen.Policy,version);
    }
}
