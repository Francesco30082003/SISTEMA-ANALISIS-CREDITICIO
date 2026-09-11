using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Integrations;
using Mapan.Application.Investigacion;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Persistence.Repositories;

// Cada fuente (buró/Equifax, judicial, aval) se resuelve por separado: si la cooperativa no activó un
// proveedor de ese tipo (integracion.empresa_proveedor), simplemente no se registra consulta para esa
// fuente. Si el proveedor está activo pero falla, se registra la consulta en estado ERROR y se continúa
// con las demás fuentes — una fuente caída nunca debe tumbar la investigación completa (ver §34 escenario J).
public sealed class InvestigacionRepository(MapanDbContext db,AuditWriter audit,ICreditBureauProvider bureau,
    IJudicialProvider judicial,IAvalProvider aval,TimeProvider clock,ILogger<InvestigacionRepository> logger) : IInvestigacionRepository
{
    public async Task<InvestigacionResultadoDto?> GetUltimaAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct))
            throw ApplicationError.NotFound();
        var consultas=await db.Set<ConsultaExterna>().AsNoTracking().Where(c=>c.EmpresaId==empresaId&&c.SolicitudCreditoId==solicitudId)
            .OrderByDescending(c=>c.FechaInicio).ToListAsync(ct);
        return consultas.Count==0?null:await BuildResultAsync(empresaId,solicitudId,consultas,ct);
    }

    public async Task<InvestigacionResultadoDto> EjecutarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct)
    {
        var solicitud=await db.Set<SolicitudCredito>().AsNoTracking().SingleOrDefaultAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct)??throw ApplicationError.NotFound();
        var cliente=await db.Set<Cliente>().AsNoTracking().SingleOrDefaultAsync(c=>c.EmpresaId==empresaId&&c.ClienteId==solicitud.ClienteId,ct)??throw ApplicationError.NotFound();

        await ConsultarBuroAsync(empresaId,membershipId,solicitudId,cliente,ct);
        await ConsultarJudicialAsync(empresaId,membershipId,solicitudId,cliente,ct);
        await ConsultarAvalAsync(empresaId,membershipId,solicitudId,cliente,ct);
        await db.SaveChangesAsync(ct);

        return (await GetUltimaAsync(empresaId,solicitudId,ct))!;
    }

    private async Task ConsultarBuroAsync(Guid empresaId,Guid membershipId,Guid solicitudId,Cliente cliente,CancellationToken ct)
    {
        var empresaProveedorId=await ResolveProviderAsync(empresaId,"BURO_CREDITO",ct);
        if(empresaProveedorId is null)return;
        var consultaId=Guid.NewGuid();
        var ahora=clock.GetUtcNow();
        try
        {
            var resultado=await bureau.ConsultarAsync(cliente.TipoIdentificacion,cliente.NumeroIdentificacion,ct);
            var json=JsonSerializer.Serialize(resultado);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="BURO_CREDITO",Estado="COMPLETADA",ReferenciaExterna="CONSULTA_API",RespuestaResumenJson=json,IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
            db.Add(new BuroSnapshot{BuroSnapshotId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                ConsultaExternaId=consultaId,FechaReporte=DateOnly.FromDateTime(ahora.UtcDateTime),ScoreBuro=resultado.Score,DeudaTotal=resultado.DeudaTotal,
                CuotaTotal=resultado.CuotaTotalMensual,CreditosActivos=resultado.DesglosePorFuente.Count,MoraActualMaxDias=resultado.MoraActualMaxDias,
                MoraHistoricaMaxDias=resultado.MoraHistoricaMaxDias,PayloadNormalizado=json});
            audit.Add("integracion.consulta_externa",consultaId,"CONSULTAR_BURO");
        }
        catch(Exception e) when(e is not OperationCanceledException)
        {
            logger.LogWarning(e,"Consulta a buró de crédito falló para la solicitud {SolicitudId}",solicitudId);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="BURO_CREDITO",Estado="ERROR",MensajeError=e.Message,IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
        }
    }

    private async Task ConsultarJudicialAsync(Guid empresaId,Guid membershipId,Guid solicitudId,Cliente cliente,CancellationToken ct)
    {
        var empresaProveedorId=await ResolveProviderAsync(empresaId,"JUDICIAL",ct);
        if(empresaProveedorId is null)return;
        var consultaId=Guid.NewGuid();
        var ahora=clock.GetUtcNow();
        try
        {
            var resultado=await judicial.ConsultarAsync(cliente.NumeroIdentificacion,ct);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="JUDICIAL",Estado="COMPLETADA",ReferenciaExterna="CONSULTA_API",RespuestaResumenJson=JsonSerializer.Serialize(resultado),IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
            audit.Add("integracion.consulta_externa",consultaId,"CONSULTAR_JUDICIAL");
        }
        catch(Exception e) when(e is not OperationCanceledException)
        {
            logger.LogWarning(e,"Consulta judicial falló para la solicitud {SolicitudId}",solicitudId);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="JUDICIAL",Estado="ERROR",MensajeError=e.Message,IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
        }
    }

    private async Task ConsultarAvalAsync(Guid empresaId,Guid membershipId,Guid solicitudId,Cliente cliente,CancellationToken ct)
    {
        var empresaProveedorId=await ResolveProviderAsync(empresaId,"AVAL",ct);
        if(empresaProveedorId is null)return;
        var consultaId=Guid.NewGuid();
        var ahora=clock.GetUtcNow();
        try
        {
            var resultado=await aval.ConsultarAsync(cliente.NumeroIdentificacion,ct);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="AVAL",Estado="COMPLETADA",ReferenciaExterna="CONSULTA_API",RespuestaResumenJson=JsonSerializer.Serialize(resultado),IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
            audit.Add("integracion.consulta_externa",consultaId,"CONSULTAR_AVAL");
        }
        catch(Exception e) when(e is not OperationCanceledException)
        {
            logger.LogWarning(e,"Consulta de aval falló para la solicitud {SolicitudId}",solicitudId);
            db.Add(new ConsultaExterna{ConsultaExternaId=consultaId,EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId.Value,
                TipoConsulta="AVAL",Estado="ERROR",MensajeError=e.Message,IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()});
        }
    }

    // Resuelve por categoría (Proveedor.Tipo), no por vendor específico: así una cooperativa puede
    // sustituir Equifax por otro buró sin que este código cambie — solo reconfigura integracion.empresa_proveedor.
    private async Task<Guid?> ResolveProviderAsync(Guid empresaId,string tipo,CancellationToken ct)=>
        await (from ep in db.Set<EmpresaProveedor>().AsNoTracking()
               join p in db.Set<Proveedor>().AsNoTracking() on ep.ProveedorId equals p.ProveedorId
               where ep.EmpresaId==empresaId&&ep.Activo&&p.Activo&&p.Tipo==tipo
               orderby ep.FechaCreacion
               select (Guid?)ep.EmpresaProveedorId).FirstOrDefaultAsync(ct);

    private async Task<InvestigacionResultadoDto> BuildResultAsync(Guid empresaId,Guid solicitudId,List<ConsultaExterna> consultas,CancellationToken ct)
    {
        var resumenes=consultas.Select(c=>new ConsultaResumenDto(c.ConsultaExternaId,c.TipoConsulta,c.Estado,c.FechaInicio,c.FechaFin,c.MensajeError,c.ReferenciaExterna)).ToList();

        BuroResumenDto? buro=null;
        var snapshot=await db.Set<BuroSnapshot>().AsNoTracking().Where(b=>b.EmpresaId==empresaId&&b.SolicitudCreditoId==solicitudId)
            .OrderByDescending(b=>b.FechaCreacion).FirstOrDefaultAsync(ct);
        if(snapshot?.PayloadNormalizado is not null)
        {
            var r=JsonSerializer.Deserialize<BuroCreditoResultado>(snapshot.PayloadNormalizado)!;
            buro=new BuroResumenDto(r.Score,r.BandaRiesgo,r.ProbabilidadMora,r.DeudaTotal,r.CuotaTotalMensual,r.OperacionesVencidas,
                r.MaximoDiasVencidoUltimos3Meses,r.MontoDemandaJudicial,r.MontoCarteraCastigada,r.MoraActualMaxDias,r.MoraHistoricaMaxDias);
        }

        JudicialResumenDto? judicialDto=null;
        var judicialConsulta=consultas.FirstOrDefault(c=>c.TipoConsulta=="JUDICIAL"&&c.Estado=="COMPLETADA");
        if(judicialConsulta?.RespuestaResumenJson is not null)
        {
            var r=JsonSerializer.Deserialize<JudicialResultado>(judicialConsulta.RespuestaResumenJson)!;
            var masGrave=r.Procesos.OrderByDescending(p=>GravedadRank(p.Gravedad)).FirstOrDefault();
            judicialDto=new JudicialResumenDto(r.TieneProcesos,r.NumeroProcesos,masGrave?.Materia,masGrave?.Gravedad);
        }

        AvalResumenDto? avalDto=null;
        var avalConsulta=consultas.FirstOrDefault(c=>c.TipoConsulta=="AVAL"&&c.Estado=="COMPLETADA");
        if(avalConsulta?.RespuestaResumenJson is not null)
        {
            var r=JsonSerializer.Deserialize<AvalResultado>(avalConsulta.RespuestaResumenJson)!;
            avalDto=new AvalResumenDto(r.EsGaranteActivo,r.OperacionesComoGarante,r.TieneMoraComoGarante);
        }

        return new(solicitudId,resumenes,buro,judicialDto,avalDto,consultas.Max(c=>c.FechaInicio));
    }

    private static int GravedadRank(string gravedad)=>gravedad switch{"ALTA"=>3,"MEDIA"=>2,"BAJA"=>1,_=>0};
}
