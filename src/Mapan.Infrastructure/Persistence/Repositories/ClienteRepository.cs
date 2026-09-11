using System.Linq.Expressions;
using Mapan.Application.Clientes;
using Mapan.Application.Common;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

public sealed class ClienteRepository(MapanDbContext db, AuditWriter audit, TimeProvider clock) : IClienteRepository
{
    private static readonly Expression<Func<Cliente, ClienteDto>> Projection = c => new(c.ClienteId, c.TipoPersona,
        c.TipoIdentificacion, c.NumeroIdentificacion, c.Nombres, c.Apellidos, c.RazonSocial, c.FechaNacimiento, c.Telefono, c.Correo, c.Estado);
    public async Task<Page<ClienteDto>> ListAsync(Guid empresaId, string? search, int page, int size, CancellationToken ct)
    {
        var query = db.Set<Cliente>().AsNoTracking().Where(c => c.EmpresaId == empresaId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.NumeroIdentificacion.Contains(search)
            || (c.Nombres != null && c.Nombres.Contains(search)) || (c.Apellidos != null && c.Apellidos.Contains(search))
            || (c.RazonSocial != null && c.RazonSocial.Contains(search)));
        return new(await query.OrderBy(c => c.NumeroIdentificacion).ThenBy(c => c.ClienteId)
            .Skip((page - 1) * size).Take(size).Select(Projection).ToListAsync(ct), await query.CountAsync(ct), page, size);
    }
    public Task<ClienteDto?> GetAsync(Guid empresaId, Guid id, CancellationToken ct) => db.Set<Cliente>()
        .AsNoTracking().Where(c => c.EmpresaId == empresaId && c.ClienteId == id).Select(Projection).SingleOrDefaultAsync(ct);
    public async Task<ClienteDto> CreateAsync(Guid empresaId, ClienteInput input, CancellationToken ct)
    {
        var entity = new Cliente { ClienteId=Guid.NewGuid(), EmpresaId=empresaId, TipoPersona=input.TipoPersona,
            TipoIdentificacion=input.TipoIdentificacion, NumeroIdentificacion=input.NumeroIdentificacion, Estado="ACTIVO" };
        Apply(entity, input); db.Add(entity); audit.Add("credito.cliente", entity.ClienteId, "CREAR");
        await db.SaveChangesAsync(ct); return (await GetAsync(empresaId, entity.ClienteId, ct))!;
    }
    public async Task<ClienteDto> UpdateAsync(Guid empresaId, Guid id, ClienteInput input, CancellationToken ct)
    {
        var entity = await FindAsync(empresaId, id, ct); Apply(entity, input);
        audit.Add("credito.cliente", id, "ACTUALIZAR"); await db.SaveChangesAsync(ct);
        return (await GetAsync(empresaId, id, ct))!;
    }
    public async Task InactivateAsync(Guid empresaId, Guid id, CancellationToken ct)
    {
        var entity = await FindAsync(empresaId, id, ct); entity.Estado="INACTIVO";
        entity.FechaActualizacion=clock.GetUtcNow(); audit.Add("credito.cliente", id, "INACTIVAR");
        await db.SaveChangesAsync(ct);
    }
    private async Task<Cliente> FindAsync(Guid empresaId, Guid id, CancellationToken ct) => await db.Set<Cliente>()
        .SingleOrDefaultAsync(c => c.EmpresaId == empresaId && c.ClienteId == id, ct) ?? throw ApplicationError.NotFound();
    private void Apply(Cliente entity, ClienteInput input)
    {
        entity.TipoPersona=input.TipoPersona; entity.TipoIdentificacion=input.TipoIdentificacion;
        entity.NumeroIdentificacion=input.NumeroIdentificacion; entity.Nombres=input.Nombres; entity.Apellidos=input.Apellidos;
        entity.RazonSocial=input.RazonSocial; entity.FechaNacimiento=input.FechaNacimiento;
        entity.Telefono=input.Telefono; entity.Correo=input.Correo; entity.FechaActualizacion=clock.GetUtcNow();
    }
}
