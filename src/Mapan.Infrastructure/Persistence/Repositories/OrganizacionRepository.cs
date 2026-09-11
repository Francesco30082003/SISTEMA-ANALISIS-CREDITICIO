using Mapan.Application.Common;
using Mapan.Application.Organizacion;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class OrganizacionRepository(MapanDbContext db,AuditWriter audit):IOrganizacionRepository
{
    public async Task<IReadOnlyList<SucursalDto>> GetSucursalesAsync(Guid empresaId,CancellationToken ct)=>await db.Set<Sucursal>().AsNoTracking().Where(s=>s.EmpresaId==empresaId&&s.Estado=="ACTIVA").OrderBy(s=>s.Codigo).Select(s=>new SucursalDto(s.SucursalId,s.Codigo,s.Nombre,s.Estado)).ToListAsync(ct);
    public async Task SetDefaultBranchAsync(Guid empresaId,Guid membershipId,Guid sucursalId,CancellationToken ct)
    {
        if(!await db.Set<Sucursal>().AnyAsync(s=>s.EmpresaId==empresaId&&s.SucursalId==sucursalId&&s.Estado=="ACTIVA",ct))throw ApplicationError.NotFound();
        var membership=await db.Set<UsuarioEmpresa>().SingleOrDefaultAsync(m=>m.EmpresaId==empresaId&&m.UsuarioEmpresaId==membershipId&&m.Estado=="ACTIVO",ct)??throw ApplicationError.Forbidden();
        membership.SucursalPredeterminadaId=sucursalId;audit.Add("seguridad.usuario_empresa",membershipId,"SUCURSAL_PREDETERMINADA");await db.SaveChangesAsync(ct);
    }
}
