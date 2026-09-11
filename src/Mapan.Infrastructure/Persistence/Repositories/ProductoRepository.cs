using System.Linq.Expressions;
using Mapan.Application.Common;
using Mapan.Application.Productos;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

public sealed class ProductoRepository(MapanDbContext db,AuditWriter audit) : IProductoRepository
{
    private static readonly Expression<Func<ProductoCredito,ProductoDto>> Projection=p=>new(p.ProductoCreditoId,p.Codigo,p.Nombre,p.Descripcion,p.MontoMinimo,p.MontoMaximo,p.PlazoMinimoMeses,p.PlazoMaximoMeses,p.MonedaCodigo,p.TasaInteresAnualPct,p.TipoTasa,p.VigenteDesde,p.VigenteHasta,p.Estado,p.Categoria);
    public async Task<IReadOnlyList<ProductoDto>> ListAsync(Guid empresaId,bool soloActivos,CancellationToken ct)=>await db.Set<ProductoCredito>().AsNoTracking().Where(p=>p.EmpresaId==empresaId&&(!soloActivos||p.Estado=="ACTIVO")).OrderBy(p=>p.Codigo).Select(Projection).ToListAsync(ct);
    public Task<ProductoDto?> GetAsync(Guid empresaId,Guid id,CancellationToken ct)=>db.Set<ProductoCredito>().AsNoTracking().Where(p=>p.EmpresaId==empresaId&&p.ProductoCreditoId==id).Select(Projection).SingleOrDefaultAsync(ct);
    public async Task<ProductoDto> CreateAsync(Guid empresaId,ProductoInput input,CancellationToken ct)
    {
        var p=new ProductoCredito {ProductoCreditoId=Guid.NewGuid(),EmpresaId=empresaId,Codigo=input.Codigo,Nombre=input.Nombre,MonedaCodigo=input.MonedaCodigo,TipoTasa="FIJA",Estado="ACTIVO"};
        Apply(p,input);db.Add(p);audit.Add("credito.producto_credito",p.ProductoCreditoId,"CREAR");await db.SaveChangesAsync(ct);return (await GetAsync(empresaId,p.ProductoCreditoId,ct))!;
    }
    public async Task<ProductoDto> UpdateAsync(Guid empresaId,Guid id,ProductoInput input,CancellationToken ct)
    {
        var p=await FindAsync(empresaId,id,ct);Apply(p,input);audit.Add("credito.producto_credito",id,"ACTUALIZAR");await db.SaveChangesAsync(ct);return (await GetAsync(empresaId,id,ct))!;
    }
    public async Task SetActiveAsync(Guid empresaId,Guid id,bool active,CancellationToken ct)
    {
        var p=await FindAsync(empresaId,id,ct);p.Estado=active?"ACTIVO":"INACTIVO";audit.Add("credito.producto_credito",id,active?"ACTIVAR":"INACTIVAR");await db.SaveChangesAsync(ct);
    }
    private async Task<ProductoCredito> FindAsync(Guid empresaId,Guid id,CancellationToken ct)=>await db.Set<ProductoCredito>().SingleOrDefaultAsync(p=>p.EmpresaId==empresaId&&p.ProductoCreditoId==id,ct)??throw ApplicationError.NotFound();
    private static void Apply(ProductoCredito p,ProductoInput i) {p.Codigo=i.Codigo;p.Nombre=i.Nombre;p.Descripcion=i.Descripcion;p.MontoMinimo=i.MontoMinimo;p.MontoMaximo=i.MontoMaximo;p.PlazoMinimoMeses=i.PlazoMinimoMeses;p.PlazoMaximoMeses=i.PlazoMaximoMeses;p.MonedaCodigo=i.MonedaCodigo;p.TasaInteresAnualPct=i.TasaInteresAnualPct;p.TipoTasa=i.TipoTasa??"FIJA";p.VigenteDesde=i.VigenteDesde;p.VigenteHasta=i.VigenteHasta;p.Categoria=i.Categoria??"OTRO";}
}
