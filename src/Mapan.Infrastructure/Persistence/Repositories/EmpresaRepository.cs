using Mapan.Application.Empresas;
using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

public sealed class EmpresaRepository(MapanDbContext dbContext, ICurrentUser currentUser) : IEmpresaRepository
{
    public async Task<IReadOnlyList<EmpresaDto>> ObtenerEmpresasAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Empresas
            .AsNoTracking()
            .Where(e => e.Estado == "ACTIVA" && dbContext.Set<UsuarioEmpresa>().Any(m =>
                m.EmpresaId == e.EmpresaId && m.UsuarioId == currentUser.UsuarioId && m.Estado == "ACTIVO"))
            .OrderBy(e => e.Codigo)
            .Select(e => new EmpresaDto(
                e.EmpresaId,
                e.Codigo,
                e.NombreLegal,
                e.NombreComercial,
                e.PaisCodigo,
                e.MonedaCodigo,
                e.Estado))
            .ToListAsync(cancellationToken);
    }
}
