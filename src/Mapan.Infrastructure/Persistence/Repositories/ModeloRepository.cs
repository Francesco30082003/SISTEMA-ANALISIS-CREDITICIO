using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Modelos;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class ModeloRepository(MapanDbContext db,AuditWriter audit):IModeloRepository
{
    private static readonly string[] ModeloStates=["BORRADOR","ACTIVO","INACTIVO","ARCHIVADO"];
    private static readonly Dictionary<string,string[]> VersionTransitions=new(){
        ["BORRADOR"]=["ENTRENADO","RETIRADO"],["ENTRENADO"]=["VALIDADO","RETIRADO"],
        ["VALIDADO"]=["PRODUCCION","SHADOW","RETIRADO"],["SHADOW"]=["PRODUCCION","RETIRADO"],
        ["PRODUCCION"]=["RETIRADO"],["RETIRADO"]=[]};

    public async Task<IReadOnlyList<Modelo>> ListAsync(Guid tenant,CancellationToken ct)=>await db.Set<Modelo>().AsNoTracking().Where(x=>x.EmpresaId==tenant).OrderBy(x=>x.Nombre).ToListAsync(ct);
    public async Task<ModeloDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct)=>new(await db.Set<Modelo>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.ModeloId==id,ct)??throw ApplicationError.NotFound(),await db.Set<ModeloVersion>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.ModeloId==id).OrderByDescending(x=>x.NumeroVersion).ToListAsync(ct));

    public async Task<Guid> SaveAsync(Guid tenant,Guid? id,ModeloInput input,CancellationToken ct)
    {
        var m=id.HasValue?await db.Set<Modelo>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.ModeloId==id,ct)??throw ApplicationError.NotFound():new Modelo{ModeloId=Guid.NewGuid(),EmpresaId=tenant,Codigo=input.Codigo,Nombre=input.Nombre,Objetivo=input.Objetivo,Estado="BORRADOR"};
        m.Codigo=input.Codigo;m.Nombre=input.Nombre;m.Descripcion=input.Descripcion;m.Objetivo=input.Objetivo;
        if(!id.HasValue)db.Add(m);audit.Add("riesgo.modelo",m.ModeloId,"GUARDAR");await db.SaveChangesAsync(ct);return m.ModeloId;
    }

    public async Task StateAsync(Guid tenant,Guid id,string state,CancellationToken ct)
    {
        if(!ModeloStates.Contains(state))throw Invalid("Estado de modelo inválido.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var m=(await db.Set<Modelo>().FromSqlInterpolated($"SELECT * FROM riesgo.modelo WHERE empresa_id={tenant} AND modelo_id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        if(m.Estado=="ARCHIVADO")throw Invalid("Un modelo archivado no se reactiva.");
        if(state=="ACTIVO"&&!await db.Set<ModeloVersion>().AnyAsync(x=>x.EmpresaId==tenant&&x.ModeloId==id,ct))throw Invalid("Crea al menos una versión antes de activar el modelo.");
        m.Estado=state;audit.Add("riesgo.modelo",id,"ESTADO_"+state);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task<Guid> CreateVersionAsync(Guid tenant,Guid id,ModeloVersionInput input,CancellationToken ct)
    {
        ValidateEsquema(input.EsquemaCaracteristicas);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        if((await db.Set<Modelo>().FromSqlInterpolated($"SELECT * FROM riesgo.modelo WHERE empresa_id={tenant} AND modelo_id={id} FOR UPDATE").ToListAsync(ct)).Count!=1)throw ApplicationError.NotFound();
        var last=await db.Set<ModeloVersion>().Where(x=>x.EmpresaId==tenant&&x.ModeloId==id).OrderByDescending(x=>x.NumeroVersion).FirstOrDefaultAsync(ct);
        var v=new ModeloVersion{ModeloVersionId=Guid.NewGuid(),EmpresaId=tenant,ModeloId=id,NumeroVersion=(last?.NumeroVersion??0)+1,Algoritmo=input.Algoritmo,Descripcion=input.Descripcion,ArtefactoUri=input.ArtefactoUri,
            EsquemaCaracteristicas=input.EsquemaCaracteristicas,Hiperparametros=input.Hiperparametros,FechaDatosDesde=input.FechaDatosDesde,FechaDatosHasta=input.FechaDatosHasta,
            CantidadRegistros=input.CantidadRegistros,CantidadPositivos=input.CantidadPositivos,CantidadNegativos=input.CantidadNegativos,RocAuc=input.RocAuc,PrecisionScore=input.PrecisionScore,
            RecallScore=input.RecallScore,F1Score=input.F1Score,AccuracyScore=input.AccuracyScore,UmbralDecision=input.UmbralDecision,Estado="BORRADOR"};
        db.Add(v);audit.Add("riesgo.modelo_version",v.ModeloVersionId,"CREAR_BORRADOR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return v.ModeloVersionId;
    }

    public async Task SaveVersionAsync(Guid tenant,Guid id,ModeloVersionInput input,CancellationToken ct)
    {
        ValidateEsquema(input.EsquemaCaracteristicas);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var v=await Lock(tenant,id,ct);
        if(v.Estado is "PRODUCCION" or "SHADOW" or "RETIRADO")throw Invalid("Esta versión ya está en uso. Crea una nueva versión para reemplazarla.");
        v.Algoritmo=input.Algoritmo;v.Descripcion=input.Descripcion;v.ArtefactoUri=input.ArtefactoUri;v.EsquemaCaracteristicas=input.EsquemaCaracteristicas;v.Hiperparametros=input.Hiperparametros;
        v.FechaDatosDesde=input.FechaDatosDesde;v.FechaDatosHasta=input.FechaDatosHasta;v.CantidadRegistros=input.CantidadRegistros;v.CantidadPositivos=input.CantidadPositivos;v.CantidadNegativos=input.CantidadNegativos;
        v.RocAuc=input.RocAuc;v.PrecisionScore=input.PrecisionScore;v.RecallScore=input.RecallScore;v.F1Score=input.F1Score;v.AccuracyScore=input.AccuracyScore;v.UmbralDecision=input.UmbralDecision;
        audit.Add("riesgo.modelo_version",id,"GUARDAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }

    public async Task VersionStateAsync(Guid tenant,Guid id,string state,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var v=await Lock(tenant,id,ct);
        if(!VersionTransitions.TryGetValue(v.Estado,out var allowed)||!allowed.Contains(state))throw Invalid($"No se puede pasar de {v.Estado} a {state}.");
        if(state is "PRODUCCION" or "SHADOW"){
            if(string.IsNullOrWhiteSpace(v.ArtefactoUri))throw Invalid("Registra la ubicación del artefacto (artefacto_uri) antes de promover esta versión.");
            if(v.UmbralDecision is null or <0 or >1)throw Invalid("Define umbral_decision entre 0 y 1 antes de promover esta versión.");
            var modelo=await db.Set<Modelo>().AsNoTracking().SingleAsync(x=>x.EmpresaId==tenant&&x.ModeloId==v.ModeloId,ct);
            if(modelo.Estado!="ACTIVO")throw Invalid("Activa el modelo antes de promover una versión.");
        }
        if(state=="PRODUCCION"&&await(from other in db.Set<ModeloVersion>() join m in db.Set<Modelo>() on other.ModeloId equals m.ModeloId
                where other.EmpresaId==tenant&&m.EmpresaId==tenant&&other.ModeloVersionId!=id&&other.Estado=="PRODUCCION"&&other.ArtefactoUri!=null&&m.Estado=="ACTIVO"
                select other.ModeloVersionId).AnyAsync(ct))
            throw Invalid("Ya existe otra versión en PRODUCCION para esta empresa. Retírala antes de promover esta.");
        v.Estado=state;
        if(state=="ENTRENADO"&&v.FechaEntrenamiento is null)v.FechaEntrenamiento=DateTimeOffset.UtcNow;
        if(state=="VALIDADO")v.FechaValidacion=DateTimeOffset.UtcNow;
        audit.Add("riesgo.modelo_version",id,"ESTADO_"+state);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }

    private async Task<ModeloVersion> Lock(Guid tenant,Guid id,CancellationToken ct)=>(await db.Set<ModeloVersion>().FromSqlInterpolated($"SELECT * FROM riesgo.modelo_version WHERE empresa_id={tenant} AND modelo_version_id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
    private static void ValidateEsquema(string json)
    {
        try{
            using var doc=JsonDocument.Parse(json);
            if(doc.RootElement.ValueKind!=JsonValueKind.Array||doc.RootElement.GetArrayLength()==0||doc.RootElement.EnumerateArray().Any(x=>x.ValueKind!=JsonValueKind.String))
                throw Invalid("esquema_caracteristicas debe ser un arreglo JSON no vacío con los nombres de variables, en el orden que espera el modelo.");
        }catch(JsonException){throw Invalid("esquema_caracteristicas debe ser JSON válido.");}
    }
    private static ApplicationError Invalid(string message)=>new(422,"MODELO_VALIDATION",message);
}
