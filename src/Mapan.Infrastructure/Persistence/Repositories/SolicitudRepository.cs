using System.Linq.Expressions;
using Mapan.Application.Common;
using Mapan.Application.Solicitudes;
using Mapan.Domain.Credito;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class SolicitudRepository(MapanDbContext db,AuditWriter audit,TimeProvider clock):ISolicitudRepository
{
    private static readonly Expression<Func<SolicitudCredito,SolicitudDto>> Projection=s=>new(s.SolicitudCreditoId,s.SucursalId,s.ClienteId,s.ProductoCreditoId,s.NumeroSolicitud,s.MontoSolicitado,s.PlazoSolicitadoMeses,s.TasaInteresAnualPct,s.CapitalMensualEstimado,s.InteresMensualEstimado,s.CuotaEstimada,s.InteresTotalEstimado,s.TotalAPagarEstimado,s.DestinoCredito,s.Estado,s.FechaCreacion);
    public async Task<Page<SolicitudDto>> ListAsync(Guid empresaId,Guid? clienteId,int page,int size,CancellationToken ct)
    {
        var q=db.Set<SolicitudCredito>().AsNoTracking().Where(s=>s.EmpresaId==empresaId&&(!clienteId.HasValue||s.ClienteId==clienteId));
        return new(await q.OrderByDescending(s=>s.FechaCreacion).ThenBy(s=>s.SolicitudCreditoId).Skip((page-1)*size).Take(size).Select(Projection).ToListAsync(ct),await q.CountAsync(ct),page,size);
    }
    public Task<SolicitudDto?> GetAsync(Guid empresaId,Guid id,CancellationToken ct)=>db.Set<SolicitudCredito>().AsNoTracking().Where(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==id).Select(Projection).SingleOrDefaultAsync(ct);
    public async Task<SolicitudDto> CreateAsync(Guid empresaId,Guid membershipId,SolicitudInput input,CancellationToken ct)
    {
        await ValidateReferencesAsync(empresaId,input,ct);
        var entity=new SolicitudCredito {SolicitudCreditoId=Guid.NewGuid(),EmpresaId=empresaId,CreadoPorUsuarioEmpresaId=membershipId,NumeroSolicitud=input.NumeroSolicitud,Estado="BORRADOR"};
        Apply(entity,input);db.Add(entity);audit.Add("credito.solicitud_credito",entity.SolicitudCreditoId,"CREAR");await db.SaveChangesAsync(ct);return (await GetAsync(empresaId,entity.SolicitudCreditoId,ct))!;
    }
    public async Task<SolicitudDto> UpdateDraftAsync(Guid empresaId,Guid id,SolicitudInput input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var entity=await LockAsync(empresaId,id,ct);
        try {SolicitudTransitions.RequireDraft(entity.Estado);}catch(InvalidOperationException e){throw new ApplicationError(422,"INVALID_TRANSITION",e.Message);}
        await ValidateReferencesAsync(empresaId,input,ct);Apply(entity,input);audit.Add("credito.solicitud_credito",id,"ACTUALIZAR_BORRADOR");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return (await GetAsync(empresaId,id,ct))!;
    }
    public async Task SendToDocumentationAsync(Guid empresaId,Guid id,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var entity=await LockAsync(empresaId,id,ct);
        try {entity.Estado=SolicitudTransitions.SendToDocumentation(entity.Estado);}catch(InvalidOperationException e){throw new ApplicationError(422,"INVALID_TRANSITION",e.Message);}
        entity.FechaEnvio=clock.GetUtcNow();audit.Add("credito.solicitud_credito",id,"ENVIAR_DOCUMENTACION");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    private async Task<SolicitudCredito> LockAsync(Guid empresaId,Guid id,CancellationToken ct)=>
        (await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id = {empresaId} AND solicitud_credito_id = {id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
    private async Task ValidateReferencesAsync(Guid empresaId,SolicitudInput input,CancellationToken ct)
    {
        if(!await db.Set<Cliente>().AnyAsync(c=>c.EmpresaId==empresaId&&c.ClienteId==input.ClienteId&&c.Estado=="ACTIVO",ct)
            ||!await db.Set<Sucursal>().AnyAsync(s=>s.EmpresaId==empresaId&&s.SucursalId==input.SucursalId&&s.Estado=="ACTIVA",ct))throw ApplicationError.NotFound();
        var product=await db.Set<ProductoCredito>().AsNoTracking().SingleOrDefaultAsync(p=>p.EmpresaId==empresaId&&p.ProductoCreditoId==input.ProductoCreditoId&&p.Estado=="ACTIVO",ct)??throw ApplicationError.NotFound();
        if(input.MontoSolicitado<product.MontoMinimo||input.MontoSolicitado>product.MontoMaximo||input.PlazoSolicitadoMeses<product.PlazoMinimoMeses||input.PlazoSolicitadoMeses>product.PlazoMaximoMeses)
            throw new ApplicationError(422,"PRODUCT_LIMITS","Monto o plazo fuera de los límites del producto.");
    }
    private static void Apply(SolicitudCredito s,SolicitudInput i) {s.SucursalId=i.SucursalId;s.ClienteId=i.ClienteId;s.ProductoCreditoId=i.ProductoCreditoId;s.NumeroSolicitud=i.NumeroSolicitud;s.MontoSolicitado=i.MontoSolicitado;s.PlazoSolicitadoMeses=i.PlazoSolicitadoMeses;s.TasaInteresAnualPct=i.TasaInteresAnualPct;s.CapitalMensualEstimado=i.CapitalMensualEstimado;s.InteresMensualEstimado=i.InteresMensualEstimado;s.CuotaEstimada=i.CuotaEstimada;s.InteresTotalEstimado=i.InteresTotalEstimado;s.TotalAPagarEstimado=i.TotalAPagarEstimado;s.DestinoCredito=i.DestinoCredito;}
}
