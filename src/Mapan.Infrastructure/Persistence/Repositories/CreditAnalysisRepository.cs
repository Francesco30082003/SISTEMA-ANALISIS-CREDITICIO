using System.Data;
using System.Text.Json;
using Mapan.Application.Analysis;
using Mapan.Application.Common;
using Mapan.Application.Integrations;
using Mapan.Domain.Analysis;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class CreditAnalysisRepository(MapanDbContext db,AuditWriter audit,IRiskModelClient modelClient,IEmailSender email,ILogger<CreditAnalysisRepository> logger):ICreditAnalysisRepository
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public async Task<Guid> ExecuteAsync(Guid tenant,Guid member,Guid request,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,ct);
        Analisis? analysis=null;
        try {
            var s=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={tenant} AND solicitud_credito_id={request} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
            if(s.Estado is "APROBADA" or "RECHAZADA" or "CANCELADA")throw Invalid("Esta solicitud está cerrada. No admite un nuevo análisis.");
            if(s.CuotaEstimada is null)throw Invalid("Confirma la cuota estimada antes de ejecutar el análisis.");
            var now=DateTimeOffset.UtcNow;
            var company=await db.Set<Empresa>().AsNoTracking().SingleAsync(x=>x.EmpresaId==tenant,ct);
            var client=await db.Set<Cliente>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.ClienteId==s.ClienteId,ct)??throw ApplicationError.NotFound();
            var product=await db.Set<ProductoCredito>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.ProductoCreditoId==s.ProductoCreditoId,ct)??throw ApplicationError.NotFound();
            var (chosenPolicy,version)=await PolicyResolution.ResolveVigenteAsync(db,tenant,s.ProductoCreditoId,now,ct);
            var chosen=new{Policy=chosenPolicy,Version=version};
            var parameters=await db.Set<Parametro>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version.PoliticaVersionId).ToListAsync(ct);
            var factor=parameters.SingleOrDefault(x=>x.Codigo=="FACTOR_CAPACIDAD");
            if(factor?.TipoDato!="NUMERO"||factor.ValorNumerico is null or <0 or >1||factor.ValorNumerico!=decimal.Round(factor.ValorNumerico.Value,6))throw Invalid("Configura FACTOR_CAPACIDAD entre 0 y 1, con hasta seis decimales.");
            var sources=await db.Set<FuenteIngreso>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).ToListAsync(ct);
            if(sources.Count==0)throw Invalid("Registra al menos una fuente y sus períodos mensuales de ingreso.");
            if(sources.Any(x=>!string.Equals(x.MonedaCodigo.Trim(),company.MonedaCodigo.Trim(),StringComparison.OrdinalIgnoreCase)))throw Invalid("Hay fuentes en otra moneda. Corrige el expediente; MAPAN no convierte monedas.");
            var sourceIds=sources.Select(x=>x.FuenteIngresoId).ToArray();
            var periods=await db.Set<IngresoPeriodo>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&sourceIds.Contains(x.FuenteIngresoId)).ToListAsync(ct);
            if(periods.Any(x=>!AnalysisDecisions.IsMonthly(x.PeriodoInicio,x.PeriodoFin)))throw Invalid("Cada período debe abarcar un mes calendario completo. Corrige las fechas de los ingresos.");
            if(periods.GroupBy(x=>new{x.FuenteIngresoId,x.PeriodoInicio}).Any(x=>x.Count()>1))throw Invalid("Una fuente tiene períodos mensuales duplicados. Corrige el expediente.");
            var expenses=await db.Set<Gasto>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).ToListAsync(ct);
            var debts=await db.Set<Obligacion>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).ToListAsync(ct);
            if(debts.Any(x=>x.SaldoActual is null||x.CuotaMensual is null))throw Invalid("Confirma saldo y cuota de cada obligación. Los datos faltantes no se consideran cero.");
            if(debts.Any(x=>!AnalysisDecisions.ObligacionEstados.Contains(x.Estado)))throw Invalid("Cada obligación debe tener un estado confirmado: VIGENTE, CANCELADA, CASTIGADA o REESTRUCTURADA.");
            var activities=await db.Set<ActividadEconomica>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).ToListAsync(ct);
            var requiredDocs=parameters.SingleOrDefault(x=>x.Codigo=="DOCUMENTOS_REQUERIDOS");
            if(requiredDocs is not null){
                if(requiredDocs.TipoDato!="JSON"||requiredDocs.ValorJson is null)throw Invalid("DOCUMENTOS_REQUERIDOS debe ser una lista JSON de tipos documentales.");
                var types=JsonSerializer.Deserialize<string[]>(requiredDocs.ValorJson)??throw Invalid("Lista documental inválida.");
                var attached=await(from l in db.Set<SolicitudDocumento>() join v in db.Set<DocumentoVersion>() on l.DocumentoVersionId equals v.DocumentoVersionId join d in db.Set<Documento>() on v.DocumentoId equals d.DocumentoId where l.EmpresaId==tenant&&v.EmpresaId==tenant&&d.EmpresaId==tenant&&l.SolicitudCreditoId==request select d.TipoDocumento).ToListAsync(ct);
                var missing=types.Except(attached).ToArray();if(missing.Length>0)throw Invalid("Adjunta los documentos requeridos: "+string.Join(", ",missing));
            }
            var calculated=new CreditFinancialAnalysisEngine().Calculate(new(sources.Select(x=>new IncomeSource(periods.Where(p=>p.FuenteIngresoId==x.FuenteIngresoId).Select(p=>p.MontoNeto).ToArray())).ToArray(),expenses.Select(x=>x.MontoMensual).ToArray(),debts.Select(x=>x.SaldoActual!.Value).ToArray(),debts.Select(x=>x.CuotaMensual!.Value).ToArray(),s.CuotaEstimada.Value,factor.ValorNumerico));
            // Señales de Fases 3/6/7: se copian al vector (no se referencian en vivo) para que este
            // análisis, ya inmutable, siga mostrando exactamente lo que existía en este momento aunque
            // el buró se vuelva a consultar o el estado de un documento cambie después.
            var buroSnapshot=await db.Set<BuroSnapshot>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).OrderByDescending(x=>x.FechaCreacion).FirstOrDefaultAsync(ct);
            var judicialConsulta=await db.Set<ConsultaExterna>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request&&x.TipoConsulta=="JUDICIAL"&&x.Estado=="COMPLETADA").OrderByDescending(x=>x.FechaInicio).FirstOrDefaultAsync(ct);
            bool? tieneProcesosJudiciales=null;string? gravedadJudicial=null;
            if(judicialConsulta?.RespuestaResumenJson is {} judicialJson&&JsonSerializer.Deserialize<JudicialResultado>(judicialJson) is {} judicialResult)
            {tieneProcesosJudiciales=judicialResult.TieneProcesos;gravedadJudicial=judicialResult.Procesos.OrderByDescending(p=>GravedadRank(p.Gravedad)).Select(p=>p.Gravedad).FirstOrDefault();}
            var documentosSospechosos=await(from l in db.Set<SolicitudDocumento>().AsNoTracking()
                join ver in db.Set<DocumentoVersion>().AsNoTracking() on l.DocumentoVersionId equals ver.DocumentoVersionId
                join d in db.Set<Documento>().AsNoTracking() on ver.DocumentoId equals d.DocumentoId
                where l.EmpresaId==tenant&&l.SolicitudCreditoId==request&&d.EmpresaId==tenant&&d.Estado=="ACTIVO"&&(d.EstadoVerificacion=="SOSPECHOSO"||d.EstadoVerificacion=="INCONSISTENTE")
                select d.DocumentoId).Distinct().CountAsync(ct);
            var ultimaVisita=await db.Set<VisitaNegocio>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).OrderByDescending(x=>x.FechaCreacion).FirstOrDefaultAsync(ct);
            var number=(await db.Set<Analisis>().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).MaxAsync(x=>(int?)x.NumeroEjecucion,ct)??0)+1;
            analysis=new(){AnalisisId=Guid.NewGuid(),EmpresaId=tenant,SolicitudCreditoId=request,PoliticaVersionId=version.PoliticaVersionId,NumeroEjecucion=number,TipoAnalisis="REGLAS",Estado="INICIADO",EjecutadoPorUsuarioEmpresaId=member,IniciadoEn=now};
            db.Add(analysis);await db.SaveChangesAsync(ct);
            var snapshot=AnalysisDecisions.Snapshot(calculated,tenant,analysis.AnalisisId,now);db.Add(snapshot);await db.SaveChangesAsync(ct);await db.Entry(snapshot).ReloadAsync(ct);
            var vector=new VectorCaracteristicas{VectorCaracteristicasId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis.AnalisisId,IngresoMensual=snapshot.IngresoTotalMensual,GastosMensuales=snapshot.GastoTotalMensual,CuotasOtrasDeudas=snapshot.CuotasActuales,DeudaTotalActual=snapshot.DeudaTotalActual,MontoSolicitado=s.MontoSolicitado,PlazoMeses=s.PlazoSolicitadoMeses,CuotaEstimada=snapshot.CuotaNuevaEstimada,MaxDiasMoraHistorico=debts.Count==0?0:debts.Max(x=>x.MaxDiasMoraHistorico),CreditosActivos=AnalysisDecisions.ActiveDebtCount(debts),AntiguedadActividadMeses=AnalysisDecisions.ActivityMonths(activities,DateOnly.FromDateTime(now.UtcDateTime)),EstabilidadIngresosScore=null,HistorialInternoScore=null,IngresoDisponible=snapshot.IngresoDisponible,CapacidadNuevaCuota=snapshot.CapacidadNuevaCuota,DeudaSobreIngreso=AnalysisDecisions.Ratio(CreditFinancialAnalysisEngine.Ratio(snapshot.DeudaTotalActual,snapshot.IngresoTotalMensual)),CuotaSobreIngreso=snapshot.RatioEndeudamientoPost,MontoSobreIngreso=AnalysisDecisions.Ratio(CreditFinancialAnalysisEngine.Ratio(s.MontoSolicitado,snapshot.IngresoTotalMensual)),
                ScoreBuro=buroSnapshot?.ScoreBuro is {} sb?(int)Math.Round(sb):null,MoraActualMaxDiasBuro=buroSnapshot?.MoraActualMaxDias,
                TieneProcesosJudiciales=tieneProcesosJudiciales,GravedadJudicial=gravedadJudicial,DocumentosSospechosos=documentosSospechosos,VisitaRecomendacion=ultimaVisita?.Recomendacion,
                FechaSnapshot=now};
            var fields=AnalysisDecisions.Fields(snapshot,vector);var ruleEngine=new RuleEngine();var outcomes=new List<RuleOutcome>();
            var rules=await db.Set<Regla>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version.PoliticaVersionId&&x.Activa&&x.Etapa=="ANALISIS").OrderBy(x=>x.Prioridad).ThenBy(x=>x.Codigo).ToListAsync(ct);
            var evaluated=new List<object>();
            foreach(var rule in rules){var matched=ruleEngine.Evaluate(rule.CondicionJson,fields);var action=ruleEngine.ParseAction(rule.AccionJson);if(!AnalysisDecisions.Results.Contains(action.Resultado))throw Invalid("La regla "+rule.Nombre+" utiliza una recomendación no admitida.");evaluated.Add(new{rule.Codigo,rule.Nombre,rule.Descripcion,rule.Severidad,rule.Prioridad,rule.CondicionJson,Resultado=action.Resultado,Aplica=matched});if(!matched)continue;
                outcomes.Add(new(rule.Severidad,rule.Prioridad,action.Resultado,rule.Descripcion??rule.Nombre));
                db.Add(new Alerta{AlertaId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis.AnalisisId,ReglaId=rule.ReglaId,Codigo=rule.Codigo,Nivel=rule.Severidad,Titulo=rule.Nombre,Descripcion=rule.Descripcion??rule.Nombre,FechaCreacion=now});
            }
            if(vector.AntiguedadActividadMeses is null)db.Add(new Alerta{AlertaId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis.AnalisisId,Codigo="CALIDAD_ACTIVIDAD_PRINCIPAL",Nivel="AMARILLO",Titulo="Revisa la actividad principal",Descripcion="No hay una única actividad principal con fecha de inicio válida. La antigüedad permanece sin dato.",FechaCreacion=now});
            db.Add(vector);
            Prediccion? prediction=null;var mlStatus="MODELO_PREDICTIVO_NO_CONFIGURADO";string? resumenRiesgo=null;IReadOnlyList<string>? sugerenciasRiesgo=null;
            var models=await(from m in db.Set<Modelo>() join v in db.Set<ModeloVersion>() on m.ModeloId equals v.ModeloId where m.EmpresaId==tenant&&v.EmpresaId==tenant&&m.Estado=="ACTIVO"&&v.Estado=="PRODUCCION"&&v.ArtefactoUri!=null select v).ToListAsync(ct);
            if(models.Count==1){try{
                // Solo campos genuinamente numéricos entran al vector del modelo ML — "gravedad_judicial" y
                // "visita_recomendacion" son categóricos y quedan disponibles únicamente para las reglas de
                // política (codificarlos a un score ordinal sería una decisión de modelado que no corresponde
                // inventar aquí sin una guía explícita de negocio).
                var featureNames=new[]{"ingreso_mensual","gastos_mensuales","cuotas_otras_deudas","deuda_total_actual","monto_solicitado","plazo_meses","cuota_estimada","max_dias_mora_historico","creditos_activos","antiguedad_actividad_meses","estabilidad_ingresos_score","historial_interno_score","ingreso_disponible","capacidad_nueva_cuota","deuda_sobre_ingreso","cuota_sobre_ingreso","monto_sobre_ingreso","score_buro","mora_actual_max_dias_buro","documentos_sospechosos"};
                var numeric=fields.Where(x=>featureNames.Contains(x.Key)).ToDictionary(x=>x.Key,x=>x.Value.ValueKind==JsonValueKind.Null?(decimal?)null:x.Value.GetDecimal());
                var response=await modelClient.PredictAsync(new(analysis.AnalisisId,models[0].ModeloVersionId,numeric),ct);
                prediction=new(){PrediccionId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis.AnalisisId,ModeloVersionId=models[0].ModeloVersionId,ProbabilidadIncumplimiento=response.ProbabilidadIncumplimiento,UmbralUtilizado=response.UmbralUtilizado,ClasePredicha=response.ClasePredicha,NivelRiesgo=response.NivelRiesgo,TiempoInferenciaMs=response.TiempoInferenciaMs,FechaPrediccion=now};db.Add(prediction);
                foreach(var f in response.Factores)db.Add(new PrediccionFactor{PrediccionFactorId=Guid.NewGuid(),PrediccionId=prediction.PrediccionId,CodigoCaracteristica=f.CodigoCaracteristica,Contribucion=f.Contribucion,Direccion=f.Direccion,FechaCreacion=now});mlStatus=response.Proveedor=="CLAUDE_TEMPORAL"?"CONFIGURADO_CLAUDE_TEMPORAL":"CONFIGURADO";analysis.TipoAnalisis="COMPLETO";resumenRiesgo=response.ResumenRiesgo;sugerenciasRiesgo=response.Sugerencias;
            }catch(Exception e) when(!ct.IsCancellationRequested&&(e is HttpRequestException or ApplicationError or TaskCanceledException or JsonException)){
                mlStatus="MODELO_PREDICTIVO_NO_DISPONIBLE";
                logger.LogWarning(e,"Predicción de riesgo no disponible para el análisis {AnalisisId} (ni ml-service ni el respaldo con Claude respondieron)",analysis.AnalisisId);
            }}
            else if(models.Count>1)mlStatus="CONFIGURACION_ML_AMBIGUA";
            var recommendation=new Recomendacion{RecomendacionId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis.AnalisisId,PrediccionId=prediction?.PrediccionId,CodigoRecomendacion=AnalysisDecisions.Recommend(snapshot.CuotaCompatible,outcomes),CumpleCapacidad=snapshot.CuotaCompatible,SeveridadMaximaReglas=outcomes.OrderBy(x=>Array.IndexOf(AnalysisDecisions.Severities,x.Severity)).FirstOrDefault()?.Severity,RequiereRevisionHumana=true,Resumen=outcomes.Count==0?"Resultado de compatibilidad de cuota con la capacidad calculada. Requiere decisión humana.":string.Join("; ",outcomes.Select(x=>x.Reason)),FechaGeneracion=now,
                DetalleJson=JsonSerializer.Serialize(new{empresa=company.NombreLegal,moneda=company.MonedaCodigo,cliente=client.RazonSocial??$"{client.Nombres} {client.Apellidos}",identificacion=client.NumeroIdentificacion,solicitud=s.NumeroSolicitud,producto=product.Nombre,monto=s.MontoSolicitado,plazo=s.PlazoSolicitadoMeses,destino=s.DestinoCredito,politica=chosen.Policy.Nombre,versionPolitica=version.NumeroVersion,mlStatus,versionModelo=prediction is null?(int?)null:models[0].NumeroVersion,reglas=evaluated,fecha=now,resumenRiesgo,sugerenciasRiesgo,ingresosPorFuente=sources.Select(x=>new{tipo=x.TipoIngreso,descripcion=x.Descripcion,promedio=periods.Where(p=>p.FuenteIngresoId==x.FuenteIngresoId).Average(p=>p.MontoNeto)}),
                    scoreBuro=vector.ScoreBuro,moraActualMaxDiasBuro=vector.MoraActualMaxDiasBuro,tieneProcesosJudiciales=vector.TieneProcesosJudiciales,gravedadJudicial=vector.GravedadJudicial,documentosSospechosos=vector.DocumentosSospechosos,visitaRecomendacion=vector.VisitaRecomendacion},Json)};
            db.Add(recommendation);await db.SaveChangesAsync(ct);
            var workflow=new WorkflowRepository(db,audit,email);
            var notification=await workflow.CreateForRecommendationAsync(tenant,member,s, recommendation,fields,ct);
            analysis.Estado="COMPLETADO";analysis.FinalizadoEn=DateTimeOffset.UtcNow;s.Estado="REVISION";
            audit.Add("riesgo.analisis",analysis.AnalisisId,"EJECUTAR");audit.Add("riesgo.recomendacion",recommendation.RecomendacionId,"GENERAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            // Fuera de la transacción a propósito: no tiene sentido mantener bloqueos de fila mientras
            // se espera al servidor SMTP, y un correo que falle no debe deshacer el análisis ya guardado.
            await workflow.NotifyAsync(tenant,notification,ct);
            return analysis.AnalisisId;
        }catch(Exception error){
            await tx.RollbackAsync(CancellationToken.None);await tx.DisposeAsync();db.ChangeTracker.Clear();
            if(analysis is not null&&!ct.IsCancellationRequested){
                await using var failureTx=await db.Database.BeginTransactionAsync(CancellationToken.None);
                await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={tenant} AND solicitud_credito_id={request} FOR UPDATE").ToListAsync(CancellationToken.None);
                analysis.NumeroEjecucion=(await db.Set<Analisis>().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==request).MaxAsync(x=>(int?)x.NumeroEjecucion)??0)+1;analysis.Estado="ERROR";analysis.FinalizadoEn=DateTimeOffset.UtcNow;db.Add(analysis);audit.Add("riesgo.analisis",analysis.AnalisisId,"ERROR");await db.SaveChangesAsync(CancellationToken.None);await failureTx.CommitAsync(CancellationToken.None);
            }
            if(error is AnalysisConfigurationException)throw Invalid(error.Message);
            throw;
        }
    }
    public async Task<IReadOnlyList<AnalysisSummary>> ListAsync(Guid tenant,Guid? request,CancellationToken ct)=>await(from a in db.Set<Analisis>().AsNoTracking() join s in db.Set<SolicitudCredito>() on a.SolicitudCreditoId equals s.SolicitudCreditoId join c in db.Set<Cliente>() on s.ClienteId equals c.ClienteId where a.EmpresaId==tenant&&s.EmpresaId==tenant&&c.EmpresaId==tenant&&(request==null||s.SolicitudCreditoId==request) orderby a.IniciadoEn descending select new AnalysisSummary(a.AnalisisId,s.NumeroSolicitud,c.RazonSocial??(c.Nombres+" "+c.Apellidos),a.Estado,a.NumeroEjecucion,a.IniciadoEn,db.Set<Recomendacion>().Where(r=>r.EmpresaId==tenant&&r.AnalisisId==a.AnalisisId).Select(r=>r.CodigoRecomendacion).FirstOrDefault())).Take(200).ToListAsync(ct);
    public async Task<AnalysisDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct)
    {
        var a=await db.Set<Analisis>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==id,ct)??throw ApplicationError.NotFound();
        var r=await db.Set<Recomendacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==id,ct);
        var alerts=await db.Set<Alerta>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.AnalisisId==id).ToListAsync(ct);
        var obligaciones=await db.Set<Obligacion>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.SolicitudCreditoId==a.SolicitudCreditoId)
            .Select(x=>new ObligacionResumen(x.Institucion,x.TipoObligacion,x.SaldoActual,x.CuotaMensual,x.DiasMoraActual,x.Estado)).ToListAsync(ct);
        var documentos=await(from l in db.Set<SolicitudDocumento>().AsNoTracking()
            join v in db.Set<DocumentoVersion>().AsNoTracking() on l.DocumentoVersionId equals v.DocumentoVersionId
            join d in db.Set<Documento>().AsNoTracking() on v.DocumentoId equals d.DocumentoId
            where l.EmpresaId==tenant&&v.EmpresaId==tenant&&d.EmpresaId==tenant&&l.SolicitudCreditoId==a.SolicitudCreditoId
            orderby d.TipoDocumento
            select new DocumentoResumen(d.TipoDocumento,v.NombreArchivo,v.NumeroVersion,l.Estado,l.Obligatorio,l.UsoDocumento)).ToListAsync(ct);
        return new(a,await db.Set<SnapshotFinanciero>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==id,ct),await db.Set<VectorCaracteristicas>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==id,ct),r,alerts.OrderBy(x=>Array.IndexOf(AnalysisDecisions.Severities,x.Nivel)).ToArray(),r?.DetalleJson is {} json?JsonSerializer.Deserialize<JsonElement>(json):null,await db.Set<Prediccion>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==id,ct),obligaciones,documentos);
    }
    public async Task<IReadOnlyList<Alerta>> AlertsAsync(Guid tenant,CancellationToken ct){var alerts=await db.Set<Alerta>().AsNoTracking().Where(x=>x.EmpresaId==tenant).OrderByDescending(x=>x.FechaCreacion).Take(500).ToListAsync(ct);return alerts.OrderBy(x=>x.Resuelta).ThenBy(x=>Array.IndexOf(AnalysisDecisions.Severities,x.Nivel)).ToArray();}
    public async Task<Guid> DecideAsync(Guid tenant,Guid member,Guid analisisId,string decision,string? comment,CancellationToken ct)
    {
        if(decision is not("APROBAR" or "RECHAZAR"))throw Invalid("Decisión no admitida.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var analysis=await db.Set<Analisis>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==analisisId,ct)??throw ApplicationError.NotFound();
        var recommendation=await db.Set<Recomendacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AnalisisId==analisisId,ct)??throw Invalid("Esta ejecución no generó una recomendación.");
        if(await db.Set<Aprobacion>().AsNoTracking().AnyAsync(x=>x.EmpresaId==tenant&&x.RecomendacionId==recommendation.RecomendacionId,ct))
            throw Invalid("Esta solicitud tiene un flujo de aprobación configurado — decide desde la bandeja de aprobaciones.");
        var request=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={tenant} AND solicitud_credito_id={analysis.SolicitudCreditoId} FOR UPDATE").ToListAsync(ct)).Single();
        if(request.Estado!="REVISION")throw Invalid("Esta solicitud no está en revisión.");
        request.Estado=decision=="APROBAR"?"APROBADA":"RECHAZADA";request.FechaFinalizacion=DateTimeOffset.UtcNow;
        db.Add(new Evento{EventoId=Guid.NewGuid(),EmpresaId=tenant,UsuarioEmpresaId=member,Origen="API",EntidadTipo="credito.solicitud_credito",
            EntidadId=request.SolicitudCreditoId.ToString(),Accion="DECISION_MANUAL_"+decision,
            DetalleJson=comment is null?null:JsonSerializer.Serialize(new{comentario=comment}),Exitoso=true,FechaEvento=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return request.SolicitudCreditoId;
    }
    public async Task ResolveAlertAsync(Guid tenant,Guid member,Guid id,CancellationToken ct){var a=await db.Set<Alerta>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.AlertaId==id,ct)??throw ApplicationError.NotFound();if(a.Resuelta)return;a.Resuelta=true;a.ResueltaPorUsuarioEmpresaId=member;a.FechaResolucion=DateTimeOffset.UtcNow;audit.Add("riesgo.alerta",id,"RESOLVER");await db.SaveChangesAsync(ct);}
    private static ApplicationError Invalid(string message)=>new(422,"ANALYSIS_VALIDATION",message);
    private static int GravedadRank(string gravedad)=>gravedad switch{"ALTA"=>3,"MEDIA"=>2,"BAJA"=>1,_=>0};
}
