using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Policies;
using Mapan.Domain.Analysis;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class PolicyRepository(MapanDbContext db,AuditWriter audit):IPolicyRepository
{
    public async Task<IReadOnlyList<PoliticaCredito>> ListAsync(Guid tenant,CancellationToken ct)=>await db.Set<PoliticaCredito>().AsNoTracking().Where(x=>x.EmpresaId==tenant).OrderBy(x=>x.Nombre).ToListAsync(ct);
    public async Task<PolicyDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct)=>new(await db.Set<PoliticaCredito>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaCreditoId==id,ct)??throw ApplicationError.NotFound(),await db.Set<PoliticaVersion>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaCreditoId==id).OrderByDescending(x=>x.NumeroVersion).ToListAsync(ct));
    public async Task<Guid> SaveAsync(Guid tenant,Guid? id,PolicyInput input,CancellationToken ct)
    {
        if(input.Estado is not("ACTIVA" or "INACTIVA"))throw Invalid("Estado de política inválido.");
        if(input.ProductoCreditoId.HasValue&&!await db.Set<ProductoCredito>().AnyAsync(x=>x.EmpresaId==tenant&&x.ProductoCreditoId==input.ProductoCreditoId,ct))throw ApplicationError.NotFound();
        var p=id.HasValue?await db.Set<PoliticaCredito>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaCreditoId==id,ct)??throw ApplicationError.NotFound():new PoliticaCredito{PoliticaCreditoId=Guid.NewGuid(),EmpresaId=tenant,Codigo=input.Codigo,Nombre=input.Nombre,Estado=input.Estado};
        p.Codigo=input.Codigo;p.Nombre=input.Nombre;p.Descripcion=input.Descripcion;p.ProductoCreditoId=input.ProductoCreditoId;p.Estado=input.Estado;
        if(!id.HasValue)db.Add(p);audit.Add("politica.politica_credito",p.PoliticaCreditoId,"GUARDAR");await db.SaveChangesAsync(ct);return p.PoliticaCreditoId;
    }
    public async Task<Guid> CreateVersionAsync(Guid tenant,Guid member,Guid id,VersionInput input,CancellationToken ct)
    {
        if(input.VigenteHasta<input.VigenteDesde)throw Invalid("El fin de vigencia no puede preceder al inicio.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        if((await db.Set<PoliticaCredito>().FromSqlInterpolated($"SELECT * FROM politica.politica_credito WHERE empresa_id={tenant} AND politica_credito_id={id} FOR UPDATE").ToListAsync(ct)).Count!=1)throw ApplicationError.NotFound();
        var last=await db.Set<PoliticaVersion>().Where(x=>x.EmpresaId==tenant&&x.PoliticaCreditoId==id).OrderByDescending(x=>x.NumeroVersion).FirstOrDefaultAsync(ct);
        var v=new PoliticaVersion{PoliticaVersionId=Guid.NewGuid(),EmpresaId=tenant,PoliticaCreditoId=id,NumeroVersion=(last?.NumeroVersion??0)+1,VigenteDesde=input.VigenteDesde.ToUniversalTime(),VigenteHasta=input.VigenteHasta?.ToUniversalTime(),Estado="BORRADOR",CreadaPorUsuarioEmpresaId=member};db.Add(v);
        if(last is not null){
            var parameters=await db.Set<Parametro>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==last.PoliticaVersionId).ToListAsync(ct);
            foreach(var p in parameters){p.ParametroId=Guid.NewGuid();p.PoliticaVersionId=v.PoliticaVersionId;db.Add(p);}
            var rules=await db.Set<Regla>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==last.PoliticaVersionId).ToListAsync(ct);
            foreach(var r in rules){r.ReglaId=Guid.NewGuid();r.PoliticaVersionId=v.PoliticaVersionId;r.FechaCreacion=DateTimeOffset.UtcNow;db.Add(r);}
        }
        audit.Add("politica.politica_version",v.PoliticaVersionId,"CREAR_BORRADOR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return v.PoliticaVersionId;
    }
    public async Task<PolicyVersionDetail> VersionAsync(Guid tenant,Guid id,CancellationToken ct)
    {
        var v=await db.Set<PoliticaVersion>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id,ct)??throw ApplicationError.NotFound();
        return new(v,await db.Set<Parametro>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id).OrderBy(x=>x.Codigo).ToListAsync(ct),await db.Set<Regla>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id).OrderBy(x=>x.Prioridad).ToListAsync(ct),v.Estado=="BORRADOR"&&!await Used(tenant,id,ct));
    }
    private Task<bool> Used(Guid tenant,Guid id,CancellationToken ct)=>db.Set<Analisis>().AnyAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id,ct);
    private async Task<PoliticaVersion> Lock(Guid tenant,Guid id,CancellationToken ct)=> (await db.Set<PoliticaVersion>().FromSqlInterpolated($"SELECT * FROM politica.politica_version WHERE empresa_id={tenant} AND politica_version_id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
    private async Task Editable(Guid tenant,Guid id,CancellationToken ct){var v=await Lock(tenant,id,ct);if(v.Estado!="BORRADOR"||await Used(tenant,id,ct))throw Invalid("Esta versión es histórica o publicada. Crea un nuevo borrador para editar.");}
    public async Task StateAsync(Guid tenant,Guid member,Guid id,string state,CancellationToken ct)
    {
        if(state is not("VIGENTE" or "INACTIVA" or "ARCHIVADA"))throw Invalid("Estado de versión inválido.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);var v=await Lock(tenant,id,ct);
        if(v.Estado=="ARCHIVADA")throw Invalid("Una versión archivada no se reactiva.");
        if(state=="VIGENTE"){
            var factor=await db.Set<Parametro>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id&&x.Codigo=="FACTOR_CAPACIDAD",ct);
            if(factor?.TipoDato!="NUMERO"||factor.ValorNumerico is null or <0 or >1)throw Invalid("Configura FACTOR_CAPACIDAD numérico entre 0 y 1.");
            var rules=await db.Set<Regla>().Where(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==id&&x.Activa).ToListAsync(ct);
            foreach(var rule in rules){
                var resultado=rule.Etapa=="PREEVALUACION"?PreevaluacionDecisions.ParseAction(rule.AccionJson):new RuleEngine().ParseAction(rule.AccionJson).Resultado;
                ValidateRule(rule.Etapa,rule.CondicionJson,resultado,rule.Severidad);
            }
            if(v.VigenteHasta<DateTimeOffset.UtcNow)throw Invalid("La vigencia ya terminó.");
            v.AprobadaPorUsuarioEmpresaId=member;v.FechaAprobacion=DateTimeOffset.UtcNow;
        }
        // Lifecycle only: parameter/rule content remains immutable after publication.
        v.Estado=state;audit.Add("politica.politica_version",id,"ESTADO_"+state);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task SaveParameterAsync(Guid tenant,Guid version,Guid? id,ParameterInput input,CancellationToken ct)
    {
        var values=new Dictionary<string,bool>{{"TEXTO",input.ValorTexto is not null},{"NUMERO",input.ValorNumerico.HasValue},{"BOOLEANO",input.ValorBooleano.HasValue},{"FECHA",input.ValorFecha.HasValue},{"JSON",input.ValorJson is not null}};
        if(!values.TryGetValue(input.TipoDato,out var present)||!present||values.Count(x=>x.Value)!=1)throw Invalid("Introduce un único valor del tipo seleccionado.");
        if(input.ValorJson is not null){try{using var json=JsonDocument.Parse(input.ValorJson);}catch(JsonException){throw Invalid("JSON inválido.");}}
        if(input.Codigo=="FACTOR_CAPACIDAD"&&(input.TipoDato!="NUMERO"||input.ValorNumerico is <0 or >1||input.ValorNumerico!=decimal.Round(input.ValorNumerico!.Value,6)))throw Invalid("FACTOR_CAPACIDAD admite de 0 a 1 con hasta seis decimales.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);await Editable(tenant,version,ct);
        var p=id.HasValue?await db.Set<Parametro>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version&&x.ParametroId==id,ct)??throw ApplicationError.NotFound():new Parametro{ParametroId=Guid.NewGuid(),EmpresaId=tenant,PoliticaVersionId=version,Codigo=input.Codigo,Nombre=input.Nombre,TipoDato=input.TipoDato};
        p.Codigo=input.Codigo;p.Nombre=input.Nombre;p.TipoDato=input.TipoDato;p.ValorTexto=input.ValorTexto;p.ValorNumerico=input.ValorNumerico;p.ValorBooleano=input.ValorBooleano;p.ValorFecha=input.ValorFecha;p.ValorJson=input.ValorJson;
        if(!id.HasValue)db.Add(p);audit.Add("politica.parametro",p.ParametroId,"GUARDAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public static void ValidateRule(string etapa,string json,string result,string severity)
    {
        if(!AnalysisDecisions.Severities.Contains(severity))throw Invalid("Severidad no admitida.");
        if(etapa=="PREEVALUACION")
        {
            if(!PreevaluacionDecisions.Acciones.Contains(result))throw Invalid("Acción de preevaluación no admitida.");
            var dummy=new PreevaluacionInputs(700,true,"BAJO",0m,0m,0,0,0m,0m,0,true,false,0,"NINGUNA",true,false,false,0,1000m,12);
            try{new RuleEngine().Evaluate(json,PreevaluacionDecisions.Fields(dummy));}catch(AnalysisConfigurationException e){throw Invalid(e.Message);}
            return;
        }
        if(etapa!="ANALISIS")throw Invalid("Etapa de regla no admitida.");
        if(!AnalysisDecisions.Results.Contains(result))throw Invalid("Resultado o severidad no admitidos.");
        var fields=AnalysisDecisions.Fields(new(){},new(){}).ToDictionary(x=>x.Key,x=>JsonSerializer.SerializeToElement<object>(x.Key=="cuota_compatible"?false:1m));
        try{new RuleEngine().Evaluate(json,fields);}catch(AnalysisConfigurationException e){throw Invalid(e.Message);}
    }
    public async Task SaveRuleAsync(Guid tenant,Guid version,Guid? id,RuleInput input,CancellationToken ct)
    {
        ValidateRule(input.Etapa,input.CondicionJson,input.Resultado,input.Severidad);await using var tx=await db.Database.BeginTransactionAsync(ct);await Editable(tenant,version,ct);
        var r=id.HasValue?await db.Set<Regla>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version&&x.ReglaId==id,ct)??throw ApplicationError.NotFound():new Regla{ReglaId=Guid.NewGuid(),EmpresaId=tenant,PoliticaVersionId=version,Codigo=input.Codigo,Nombre=input.Nombre,Severidad=input.Severidad,Etapa=input.Etapa,CondicionJson=input.CondicionJson,AccionJson="{}"};
        r.Codigo=input.Codigo;r.Nombre=input.Nombre;r.Descripcion=input.Descripcion;r.Prioridad=input.Prioridad;r.Severidad=input.Severidad;r.Etapa=input.Etapa;r.CondicionJson=input.CondicionJson;
        r.AccionJson=input.Etapa=="PREEVALUACION"?JsonSerializer.Serialize(new{accion=input.Resultado}):JsonSerializer.Serialize(new{accion="GENERAR_ALERTA",resultado=input.Resultado});
        r.Activa=input.Activa;
        if(!id.HasValue)db.Add(r);audit.Add("politica.regla",r.ReglaId,"GUARDAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task DeleteItemAsync(Guid tenant,Guid version,Guid id,bool rule,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);await Editable(tenant,version,ct);
        if(rule)db.Remove(await db.Set<Regla>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version&&x.ReglaId==id,ct)??throw ApplicationError.NotFound());
        else db.Remove(await db.Set<Parametro>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PoliticaVersionId==version&&x.ParametroId==id,ct)??throw ApplicationError.NotFound());
        audit.Add(rule?"politica.regla":"politica.parametro",id,"ELIMINAR_BORRADOR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    private static ApplicationError Invalid(string message)=>new(422,"POLICY_VALIDATION",message);
}
