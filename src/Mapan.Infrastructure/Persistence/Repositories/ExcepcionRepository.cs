using Mapan.Application.Common;
using Mapan.Application.Excepciones;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

public sealed class ExcepcionRepository(MapanDbContext db,AuditWriter audit,TimeProvider clock) : IExcepcionRepository
{
    public async Task<IReadOnlyList<ExcepcionDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)=>await
        (from e in db.Set<Excepcion>().AsNoTracking()
         join r in db.Set<Regla>().AsNoTracking() on e.ReglaId equals r.ReglaId into rj from r in rj.DefaultIfEmpty()
         join sol in db.Set<UsuarioEmpresa>().AsNoTracking() on e.SolicitadaPorUsuarioEmpresaId equals sol.UsuarioEmpresaId into solj from sol in solj.DefaultIfEmpty()
         join solu in db.Set<Usuario>().AsNoTracking() on sol.UsuarioId equals solu.UsuarioId into soluj from solu in soluj.DefaultIfEmpty()
         join res in db.Set<UsuarioEmpresa>().AsNoTracking() on e.ResueltaPorUsuarioEmpresaId equals res.UsuarioEmpresaId into resj from res in resj.DefaultIfEmpty()
         join resu in db.Set<Usuario>().AsNoTracking() on res.UsuarioId equals resu.UsuarioId into resuj from resu in resuj.DefaultIfEmpty()
         where e.EmpresaId==empresaId&&e.SolicitudCreditoId==solicitudId
         orderby e.FechaSolicitud descending
         select new ExcepcionDto(e.ExcepcionId,e.SolicitudCreditoId,e.ReglaId,r==null?null:r.Nombre,e.MotivoJustificacion,e.Observacion,e.Evidencia,
            e.Estado,e.SolicitadaPorUsuarioEmpresaId,solu==null?null:solu.NombreUsuario,e.FechaSolicitud,
            e.ResueltaPorUsuarioEmpresaId,resu==null?null:resu.NombreUsuario,e.FechaResolucion,e.ComentarioResolucion)
        ).ToListAsync(ct);

    public async Task<Guid> SolicitarAsync(Guid empresaId,Guid membershipId,ExcepcionInput input,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==input.SolicitudCreditoId,ct))
            throw ApplicationError.NotFound();
        if(input.ReglaId.HasValue&&!await db.Set<Regla>().AsNoTracking().AnyAsync(r=>r.EmpresaId==empresaId&&r.ReglaId==input.ReglaId,ct))
            throw ApplicationError.NotFound();
        var entity=new Excepcion{ExcepcionId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=input.SolicitudCreditoId,ReglaId=input.ReglaId,
            MotivoJustificacion=input.MotivoJustificacion,Observacion=input.Observacion,Evidencia=input.Evidencia,Estado="SOLICITADA",
            SolicitadaPorUsuarioEmpresaId=membershipId,FechaSolicitud=clock.GetUtcNow()};
        db.Add(entity);audit.Add("politica.excepcion",entity.ExcepcionId,"SOLICITAR");await db.SaveChangesAsync(ct);
        return entity.ExcepcionId;
    }

    public async Task ResolverAsync(Guid empresaId,Guid membershipId,Guid id,bool aprobar,string? comentario,CancellationToken ct)
    {
        var entity=await db.Set<Excepcion>().SingleOrDefaultAsync(e=>e.EmpresaId==empresaId&&e.ExcepcionId==id,ct)??throw ApplicationError.NotFound();
        if(entity.Estado!="SOLICITADA")throw new ApplicationError(422,"EXCEPCION_VALIDATION","Esta excepción ya fue resuelta.");
        entity.Estado=aprobar?"APROBADA":"RECHAZADA";entity.ResueltaPorUsuarioEmpresaId=membershipId;entity.FechaResolucion=clock.GetUtcNow();entity.ComentarioResolucion=comentario;
        audit.Add("politica.excepcion",entity.ExcepcionId,aprobar?"APROBAR":"RECHAZAR");await db.SaveChangesAsync(ct);
    }
}
