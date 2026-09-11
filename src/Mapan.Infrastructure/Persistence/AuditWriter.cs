using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Mapan.Infrastructure.Persistence;

public sealed class AuditWriter(MapanDbContext db, ICurrentUser user, ICurrentTenant tenant,
    IHttpContextAccessor http, TimeProvider clock)
{
    public void Add(string entity, Guid id, string action)
    {
        db.Set<Evento>().Add(new Evento {
            EmpresaId = tenant.EmpresaId, UsuarioId = user.UsuarioId, UsuarioEmpresaId = tenant.UsuarioEmpresaId,
            CorrelationId = http.HttpContext?.TraceIdentifier, Origen = "API", EntidadTipo = entity,
            EntidadId = id.ToString(), Accion = action, Exitoso = true, FechaEvento = clock.GetUtcNow()
        });
    }
}
