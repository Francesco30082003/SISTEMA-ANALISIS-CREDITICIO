using Mapan.Application.Common;
using Mapan.Application.Visitas;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

// Visita de negocio en campo (Fase 7): historial simple, sin motor de reglas ni proveedor externo — es
// el juicio directo del analista/oficial que hizo la visita, registrado y auditado como cualquier otro
// paso del expediente. La evidencia fotográfica se sube por el módulo de Documentos ya existente
// (tipo "Foto de visita de negocio"), no se duplica almacenamiento aquí.
public sealed class VisitaNegocioRepository(MapanDbContext db,AuditWriter audit,TimeProvider clock) : IVisitaNegocioRepository
{
    public async Task<IReadOnlyList<VisitaNegocioDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)=>await
        (from v in db.Set<VisitaNegocio>().AsNoTracking()
         join ue in db.Set<UsuarioEmpresa>().AsNoTracking() on new{v.EmpresaId,v.RealizadaPorUsuarioEmpresaId} equals new{ue.EmpresaId,RealizadaPorUsuarioEmpresaId=ue.UsuarioEmpresaId}
         join u in db.Set<Usuario>().AsNoTracking() on ue.UsuarioId equals u.UsuarioId
         where v.EmpresaId==empresaId&&v.SolicitudCreditoId==solicitudId
         orderby v.FechaCreacion descending
         select new VisitaNegocioDto(v.VisitaNegocioId,v.SolicitudCreditoId,v.FechaVisita,v.DireccionObservada,v.Latitud,v.Longitud,
            v.NegocioExiste,v.TipoNegocioObservado,v.TiempoFuncionamientoObservado,v.NumeroEmpleadosObservado,v.InventarioEstimado,
            v.IngresoMensualEstimadoObservado,v.Observaciones,v.Recomendacion,v.RealizadaPorUsuarioEmpresaId,u.Nombres+" "+u.Apellidos,v.FechaCreacion)
        ).ToListAsync(ct);

    public async Task<Guid> RegistrarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,VisitaNegocioInput input,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct))
            throw ApplicationError.NotFound();
        var entity=new VisitaNegocio{VisitaNegocioId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,
            FechaVisita=input.FechaVisita,DireccionObservada=input.DireccionObservada,Latitud=input.Latitud,Longitud=input.Longitud,
            NegocioExiste=input.NegocioExiste,TipoNegocioObservado=input.TipoNegocioObservado,TiempoFuncionamientoObservado=input.TiempoFuncionamientoObservado,
            NumeroEmpleadosObservado=input.NumeroEmpleadosObservado,InventarioEstimado=input.InventarioEstimado,
            IngresoMensualEstimadoObservado=input.IngresoMensualEstimadoObservado,Observaciones=input.Observaciones,
            Recomendacion=input.Recomendacion,RealizadaPorUsuarioEmpresaId=membershipId,FechaCreacion=clock.GetUtcNow()};
        db.Add(entity);audit.Add("credito.visita_negocio",entity.VisitaNegocioId,"REGISTRAR");await db.SaveChangesAsync(ct);
        return entity.VisitaNegocioId;
    }
}
