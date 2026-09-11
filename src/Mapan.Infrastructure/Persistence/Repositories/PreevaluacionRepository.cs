using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Investigacion;
using Mapan.Application.Preevaluacion;
using Mapan.Domain.Analysis;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

// La preevaluación reutiliza el resultado ya persistido de la Investigación del Cliente (Fase 3) —
// nunca vuelve a consultar Equifax/judicial/aval por su cuenta — y la misma política vigente que usa
// el análisis financiero (PolicyResolution), pero evaluando solo las reglas marcadas Etapa=PREEVALUACION.
public sealed class PreevaluacionRepository(MapanDbContext db,AuditWriter audit,IInvestigacionRepository investigacion,TimeProvider clock) : IPreevaluacionRepository
{
    public async Task<PreevaluacionResumenDto?> GetUltimaAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct))
            throw ApplicationError.NotFound();
        var last=await db.Set<Preevaluacion>().AsNoTracking().Where(p=>p.EmpresaId==empresaId&&p.SolicitudCreditoId==solicitudId)
            .OrderByDescending(p=>p.FechaEjecucion).FirstOrDefaultAsync(ct);
        return last is null?null:Map(last);
    }

    public async Task<PreevaluacionResumenDto> EjecutarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct)
    {
        var s=await db.Set<SolicitudCredito>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.SolicitudCreditoId==solicitudId,ct)??throw ApplicationError.NotFound();
        if(s.Estado is "APROBADA" or "RECHAZADA" or "CANCELADA")throw Invalid("Esta solicitud está cerrada. No admite una nueva preevaluación.");
        var inv=await investigacion.GetUltimaAsync(empresaId,solicitudId,ct)??throw Invalid("Ejecuta la investigación del cliente antes de la preevaluación.");
        var now=clock.GetUtcNow();
        var (_,version)=await PolicyResolution.ResolveVigenteAsync(db,empresaId,s.ProductoCreditoId,now,ct);
        var fields=PreevaluacionDecisions.Fields(BuildInputs(inv,s));
        var rules=await db.Set<Regla>().AsNoTracking().Where(x=>x.EmpresaId==empresaId&&x.PoliticaVersionId==version.PoliticaVersionId&&x.Activa&&x.Etapa=="PREEVALUACION")
            .OrderBy(x=>x.Prioridad).ThenBy(x=>x.Codigo).ToListAsync(ct);
        var engine=new RuleEngine();var outcomes=new List<PreevaluacionOutcome>();var evaluated=new List<object>();
        foreach(var rule in rules)
        {
            var matched=engine.Evaluate(rule.CondicionJson,fields);
            var accion=PreevaluacionDecisions.ParseAction(rule.AccionJson);
            evaluated.Add(new{rule.Codigo,rule.Nombre,rule.Descripcion,rule.Severidad,Accion=accion,Aplica=matched});
            if(matched)outcomes.Add(new(accion,rule.Severidad,rule.Prioridad,rule.Descripcion??rule.Nombre));
        }
        var resultado=PreevaluacionDecisions.Recommend(outcomes);
        var severidadMaxima=outcomes.OrderBy(x=>Array.IndexOf(AnalysisDecisions.Severities,x.Severidad)).FirstOrDefault()?.Severidad;
        var entity=new Preevaluacion{PreevaluacionId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,PoliticaVersionId=version.PoliticaVersionId,
            ResultadoPreliminar=resultado,SeveridadMaxima=severidadMaxima,EjecutadaPorUsuarioEmpresaId=membershipId,FechaEjecucion=now,
            DetalleJson=JsonSerializer.Serialize(new{reglas=evaluated,fecha=now})};
        db.Add(entity);audit.Add("politica.preevaluacion",entity.PreevaluacionId,"EJECUTAR");await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private static PreevaluacionInputs BuildInputs(InvestigacionResultadoDto inv,SolicitudCredito s)
    {
        var b=inv.Buro;var j=inv.Judicial;var a=inv.Aval;
        return new(
            b?.Score,b is not null,b?.BandaRiesgo??"SIN_DATOS",b?.DeudaTotal??0m,b?.CuotaTotalMensual??0m,
            b?.OperacionesVencidas??0,b?.MaximoDiasVencidoUltimos3Meses??0,b?.MontoDemandaJudicial??0m,b?.MontoCarteraCastigada??0m,
            b?.MoraActualMaxDias,
            j is not null,j?.TieneProcesos??false,j?.NumeroProcesos??0,j?.GravedadMaxima??"NINGUNA",
            a is not null,a?.EsGaranteActivo??false,a?.TieneMoraComoGarante??false,a?.OperacionesComoGarante??0,
            s.MontoSolicitado,s.PlazoSolicitadoMeses);
    }

    private static PreevaluacionResumenDto Map(Preevaluacion p)
    {
        var reglas=new List<ReglaEvaluadaDto>();
        if(p.DetalleJson is not null)
        {
            using var doc=JsonDocument.Parse(p.DetalleJson);
            if(doc.RootElement.TryGetProperty("reglas",out var arr))
                foreach(var r in arr.EnumerateArray())
                    reglas.Add(new(r.GetProperty("Codigo").GetString()!,r.GetProperty("Nombre").GetString()!,
                        r.TryGetProperty("Descripcion",out var d)&&d.ValueKind==JsonValueKind.String?d.GetString():null,
                        r.GetProperty("Severidad").GetString()!,r.GetProperty("Accion").GetString()!,r.GetProperty("Aplica").GetBoolean()));
        }
        return new(p.SolicitudCreditoId,p.ResultadoPreliminar,p.SeveridadMaxima,reglas,p.FechaEjecucion);
    }
    private static ApplicationError Invalid(string message)=>new(422,"PREEVALUACION_VALIDATION",message);
}
