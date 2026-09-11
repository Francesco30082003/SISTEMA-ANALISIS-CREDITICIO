using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Domain.Analysis;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class WorkflowRepository(MapanDbContext db,AuditWriter audit,IEmailSender email)
{
    // Quién debe actuar ahora en un paso de aprobación recién activado (para avisarle por correo) —
    // devuelto por CreateForRecommendationAsync/DecideAsync en vez de enviar el correo dentro de su
    // propia transacción, para no mantener bloqueos de fila mientras se espera al servidor SMTP.
    public sealed record StepNotification(Guid RolId,string NombrePaso,string NumeroSolicitud);

    public async Task<StepNotification?> CreateForRecommendationAsync(Guid tenant,Guid member,SolicitudCredito request,Recomendacion recommendation,Dictionary<string,JsonElement> fields,CancellationToken ct)
    {
        fields=new(fields){["recomendacion"]=JsonSerializer.SerializeToElement(recommendation.CodigoRecomendacion)};
        var routes=await db.Set<RutaAprobacion>().Where(x=>x.EmpresaId==tenant&&x.Activa&&(x.ProductoCreditoId==null||x.ProductoCreditoId==request.ProductoCreditoId)).ToListAsync(ct);
        var matches=new List<RutaAprobacion>();
        foreach(var route in routes){var rules=await db.Set<ReglaEnrutamiento>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.RutaAprobacionId==route.RutaAprobacionId&&x.Activa).OrderBy(x=>x.Prioridad).ToListAsync(ct);var matched=false;foreach(var rule in rules)matched|=new RuleEngine().Evaluate(rule.CondicionJson,fields);if(matched)matches.Add(route);}
        if(matches.Count==0)matches=routes.Where(x=>x.EsPredeterminada).ToList();
        if(matches.Count==0)return null;
        if(matches.Any(x=>x.ProductoCreditoId==request.ProductoCreditoId))matches=matches.Where(x=>x.ProductoCreditoId==request.ProductoCreditoId).ToList();
        var priority=matches.Min(x=>x.Prioridad);matches=matches.Where(x=>x.Prioridad==priority).ToList();
        if(matches.Count!=1)throw Invalid("Hay rutas de aprobación con la misma prioridad. Revisa su configuración.");
        var routeId=matches[0].RutaAprobacionId;
        await db.Set<RutaAprobacion>().FromSqlInterpolated($"SELECT * FROM flujo.ruta_aprobacion WHERE empresa_id={tenant} AND ruta_aprobacion_id={routeId} FOR SHARE").ToListAsync(ct);
        var steps=await db.Set<RutaAprobacionPaso>().Where(x=>x.EmpresaId==tenant&&x.RutaAprobacionId==routeId).OrderBy(x=>x.Orden).ToListAsync(ct);
        if(steps.Count==0)throw Invalid("La ruta aplicable no tiene pasos configurados.");
        var roles=steps.Select(x=>x.RolId).Distinct().ToArray();if(await db.Set<Rol>().CountAsync(x=>x.EmpresaId==tenant&&x.Estado=="ACTIVO"&&roles.Contains(x.RolId),ct)!=roles.Length)throw Invalid("La ruta contiene roles inactivos o de otra empresa.");
        var approval=new Aprobacion{AprobacionId=Guid.NewGuid(),EmpresaId=tenant,RecomendacionId=recommendation.RecomendacionId,RutaAprobacionId=routeId,Estado="EN_CURSO",CreadaPorUsuarioEmpresaId=member,FechaInicio=DateTimeOffset.UtcNow};db.Add(approval);
        foreach(var step in steps)db.Add(new AprobacionPaso{AprobacionPasoId=Guid.NewGuid(),EmpresaId=tenant,AprobacionId=approval.AprobacionId,RutaAprobacionPasoId=step.RutaAprobacionPasoId,Orden=step.Orden,NombrePaso=step.Nombre,RolId=step.RolId,CantidadAprobacionesRequeridas=step.CantidadAprobacionesRequeridas,Estado=step==steps[0]?"ACTIVO":"PENDIENTE",FechaHabilitacion=step==steps[0]?DateTimeOffset.UtcNow:null});
        audit.Add("flujo.aprobacion",approval.AprobacionId,"CREAR");
        return new StepNotification(steps[0].RolId,steps[0].Nombre,request.NumeroSolicitud);
    }
    public async Task<object> InboxAsync(Guid tenant,Guid member,CancellationToken ct)=>await(from step in db.Set<AprobacionPaso>().AsNoTracking() join a in db.Set<Aprobacion>() on step.AprobacionId equals a.AprobacionId join r in db.Set<Recomendacion>() on a.RecomendacionId equals r.RecomendacionId join analysis in db.Set<Analisis>() on r.AnalisisId equals analysis.AnalisisId join s in db.Set<SolicitudCredito>() on analysis.SolicitudCreditoId equals s.SolicitudCreditoId join role in db.Set<Rol>() on step.RolId equals role.RolId where step.EmpresaId==tenant&&a.EmpresaId==tenant&&r.EmpresaId==tenant&&analysis.EmpresaId==tenant&&s.EmpresaId==tenant&&role.EmpresaId==tenant&&step.Estado=="ACTIVO"&&db.Set<UsuarioEmpresaRol>().Any(x=>x.UsuarioEmpresaId==member&&x.RolId==step.RolId)&&!db.Set<DecisionPaso>().Any(x=>x.EmpresaId==tenant&&x.AprobacionPasoId==step.AprobacionPasoId&&x.UsuarioEmpresaId==member) orderby a.FechaInicio select new{step.AprobacionPasoId,step.NombrePaso,step.Estado,step.CantidadAprobacionesRequeridas,Solicitud=s.NumeroSolicitud,analysis.AnalisisId,Rol=role.Nombre,Recomendacion=r.CodigoRecomendacion,step.FechaHabilitacion,AnalisisIniciadoEn=analysis.IniciadoEn}).Take(200).ToListAsync(ct);
    public async Task<(Guid DecisionPasoId,StepNotification? Siguiente)> DecideAsync(Guid tenant,Guid member,Guid id,string decision,string? comment,CancellationToken ct)
    {
        if(decision is not("APROBAR" or "RECHAZAR" or "DEVOLVER" or "ABSTENERSE"))throw Invalid("Decisión no admitida.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var step=(await db.Set<AprobacionPaso>().FromSqlInterpolated($"SELECT * FROM flujo.aprobacion_paso WHERE empresa_id={tenant} AND aprobacion_paso_id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        var a=(await db.Set<Aprobacion>().FromSqlInterpolated($"SELECT * FROM flujo.aprobacion WHERE empresa_id={tenant} AND aprobacion_id={step.AprobacionId} FOR UPDATE").ToListAsync(ct)).Single();
        if(step.Estado!="ACTIVO"||a.Estado!="EN_CURSO")throw Invalid("Este paso ya no admite decisiones.");
        if(!await(from ur in db.Set<UsuarioEmpresaRol>() join role in db.Set<Rol>() on ur.RolId equals role.RolId where ur.UsuarioEmpresaId==member&&role.EmpresaId==tenant&&role.RolId==step.RolId&&role.Estado=="ACTIVO" select ur).AnyAsync(ct))throw ApplicationError.Forbidden();
        if(await db.Set<DecisionPaso>().AnyAsync(x=>x.EmpresaId==tenant&&x.AprobacionPasoId==id&&x.UsuarioEmpresaId==member,ct))throw Invalid("Tu decisión ya fue registrada.");
        var config=await db.Set<RutaAprobacionPaso>().SingleAsync(x=>x.EmpresaId==tenant&&x.RutaAprobacionPasoId==step.RutaAprobacionPasoId,ct);
        if((decision=="APROBAR"&&!config.PermiteAprobar)||(decision=="RECHAZAR"&&!config.PermiteRechazar)||(decision=="DEVOLVER"&&!config.PermiteDevolver))throw ApplicationError.Forbidden();
        var d=new DecisionPaso{DecisionPasoId=Guid.NewGuid(),EmpresaId=tenant,AprobacionPasoId=id,UsuarioEmpresaId=member,Decision=decision,Comentario=comment,FechaDecision=DateTimeOffset.UtcNow};db.Add(d);await db.SaveChangesAsync(ct);
        if(decision=="RECHAZAR"||decision=="DEVOLVER"){step.Estado=decision=="RECHAZAR"?"RECHAZADO":"DEVUELTO";a.Estado=decision=="RECHAZAR"?"RECHAZADA":"DEVUELTA";}
        StepNotification? siguiente=null;
        if(decision=="APROBAR"&&await db.Set<DecisionPaso>().CountAsync(x=>x.EmpresaId==tenant&&x.AprobacionPasoId==id&&x.Decision=="APROBAR",ct)>=step.CantidadAprobacionesRequeridas){
            step.Estado="APROBADO";var next=await db.Set<AprobacionPaso>().Where(x=>x.EmpresaId==tenant&&x.AprobacionId==a.AprobacionId&&x.Estado=="PENDIENTE").OrderBy(x=>x.Orden).FirstOrDefaultAsync(ct);
            if(next is null)a.Estado="APROBADA";else{
                next.Estado="ACTIVO";next.FechaHabilitacion=DateTimeOffset.UtcNow;
                var numero=await(from r in db.Set<Recomendacion>() join an in db.Set<Analisis>() on r.AnalisisId equals an.AnalisisId join s in db.Set<SolicitudCredito>() on an.SolicitudCreditoId equals s.SolicitudCreditoId where r.EmpresaId==tenant&&an.EmpresaId==tenant&&s.EmpresaId==tenant&&r.RecomendacionId==a.RecomendacionId select s.NumeroSolicitud).SingleAsync(ct);
                siguiente=new StepNotification(next.RolId,next.NombrePaso,numero);
            }
        }
        if(step.Estado!="ACTIVO")step.FechaCompletado=DateTimeOffset.UtcNow;
        if(a.Estado!="EN_CURSO"){
            a.FechaFin=DateTimeOffset.UtcNow;
            var request=await(from r in db.Set<Recomendacion>() join an in db.Set<Analisis>() on r.AnalisisId equals an.AnalisisId join s in db.Set<SolicitudCredito>() on an.SolicitudCreditoId equals s.SolicitudCreditoId where r.EmpresaId==tenant&&an.EmpresaId==tenant&&s.EmpresaId==tenant&&r.RecomendacionId==a.RecomendacionId select s).SingleAsync(ct);
            request.Estado=a.Estado=="DEVUELTA"?"BORRADOR":a.Estado;request.FechaFinalizacion=a.Estado=="DEVUELTA"?null:DateTimeOffset.UtcNow;
        }
        audit.Add("flujo.decision_paso",d.DecisionPasoId,"DECISION_"+decision);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return (d.DecisionPasoId,siguiente);
    }
    // Avisa por correo a todo usuario activo con el rol responsable de un paso recién activado — el
    // analista que ejecutó el análisis no tiene por qué ir a preguntar manualmente quién sigue.
    public async Task NotifyAsync(Guid tenant,StepNotification? notification,CancellationToken ct)
    {
        if(notification is null)return;
        var destinatarios=await(from ur in db.Set<UsuarioEmpresaRol>().AsNoTracking()
            join ue in db.Set<UsuarioEmpresa>().AsNoTracking() on ur.UsuarioEmpresaId equals ue.UsuarioEmpresaId
            join r in db.Set<Rol>().AsNoTracking() on ur.RolId equals r.RolId
            join u in db.Set<Usuario>().AsNoTracking() on ue.UsuarioId equals u.UsuarioId
            where ue.EmpresaId==tenant&&r.EmpresaId==tenant&&r.RolId==notification.RolId&&r.Estado=="ACTIVO"&&ue.Estado=="ACTIVO"
            select u.Correo).Distinct().ToListAsync(ct);
        if(destinatarios.Count==0)return;
        var body=EmailTemplate.Wrap("Aprobaciones pendientes","Una solicitud espera tu revisión",
            $"La solicitud <strong>{EmailTemplate.Escape(notification.NumeroSolicitud)}</strong> avanzó al paso "+
            $"\"{EmailTemplate.Escape(notification.NombrePaso)}\" y necesita tu decisión. Revísala desde la bandeja de Aprobaciones en MAPAN.");
        foreach(var correo in destinatarios)await email.SendAsync(correo,$"MAPAN: {notification.NumeroSolicitud} espera tu aprobación",body,ct);
    }
    private static ApplicationError Invalid(string message)=>new(422,"WORKFLOW_VALIDATION",message);
}
