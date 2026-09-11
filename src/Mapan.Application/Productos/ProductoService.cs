using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Credito;

namespace Mapan.Application.Productos;

/// <summary>Tipos de tasa soportados hoy. Se reserva el campo para diferenciar fija/variable más adelante sin migrar datos.</summary>
public static class TiposTasa { public const string Fija="FIJA"; public const string Variable="VARIABLE"; public static readonly string[] Validos=[Fija,Variable]; }
/// <summary>Categoría del producto — determina, entre otras cosas, si la visita de negocio (Fase 7) aplica típicamente.</summary>
public static class CategoriasProducto { public const string Microcredito="MICROCREDITO"; public const string Comercial="COMERCIAL"; public const string Consumo="CONSUMO"; public const string Otro="OTRO"; public static readonly string[] Validas=[Microcredito,Comercial,Consumo,Otro]; }

public sealed record ProductoInput([Required,MaxLength(40)] string Codigo,[Required,MaxLength(150)] string Nombre,
    [MaxLength(500)] string? Descripcion,decimal? MontoMinimo,decimal? MontoMaximo,int? PlazoMinimoMeses,
    int? PlazoMaximoMeses,[Required,StringLength(3,MinimumLength=3)] string MonedaCodigo,
    decimal? TasaInteresAnualPct,string? TipoTasa,DateOnly? VigenteDesde,DateOnly? VigenteHasta,string? Categoria);
public sealed record ProductoDto(Guid ProductoCreditoId,string Codigo,string Nombre,string? Descripcion,
    decimal? MontoMinimo,decimal? MontoMaximo,int? PlazoMinimoMeses,int? PlazoMaximoMeses,string MonedaCodigo,
    decimal? TasaInteresAnualPct,string? TipoTasa,DateOnly? VigenteDesde,DateOnly? VigenteHasta,string Estado,string Categoria);
public sealed record SimularInput(decimal MontoSolicitado,int PlazoMeses);
public interface IProductoRepository
{
    Task<IReadOnlyList<ProductoDto>> ListAsync(Guid empresaId,bool soloActivos,CancellationToken ct);
    Task<ProductoDto?> GetAsync(Guid empresaId,Guid id,CancellationToken ct);
    Task<ProductoDto> CreateAsync(Guid empresaId,ProductoInput input,CancellationToken ct);
    Task<ProductoDto> UpdateAsync(Guid empresaId,Guid id,ProductoInput input,CancellationToken ct);
    Task SetActiveAsync(Guid empresaId,Guid id,bool active,CancellationToken ct);
}
public sealed class ProductoService(IProductoRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<ProductoDto>> ListAsync(bool soloActivos,CancellationToken ct) {permissions.Require("Productos:Read");return repository.ListAsync(tenant.EmpresaId,soloActivos,ct);}
    public async Task<ProductoDto> GetAsync(Guid id,CancellationToken ct) {permissions.Require("Productos:Read");return await repository.GetAsync(tenant.EmpresaId,id,ct)??throw ApplicationError.NotFound();}
    public Task<ProductoDto> CreateAsync(ProductoInput input,CancellationToken ct) {permissions.Require("Productos:Create");Validate(input);return repository.CreateAsync(tenant.EmpresaId,input,ct);}
    public Task<ProductoDto> UpdateAsync(Guid id,ProductoInput input,CancellationToken ct) {permissions.Require("Productos:Update");Validate(input);return repository.UpdateAsync(tenant.EmpresaId,id,input,ct);}
    public Task SetActiveAsync(Guid id,bool active,CancellationToken ct) {permissions.Require("Productos:Update");return repository.SetActiveAsync(tenant.EmpresaId,id,active,ct);}
    public async Task<CreditoCalculoResultado> SimularAsync(Guid id,SimularInput input,CancellationToken ct)
    {
        permissions.Require("Productos:Read");
        if(input.MontoSolicitado<=0||input.PlazoMeses<=0) throw new ApplicationError(400,"VALIDATION","Monto y plazo deben ser mayores a cero.");
        var producto=await repository.GetAsync(tenant.EmpresaId,id,ct)??throw ApplicationError.NotFound();
        RequireTasaVigente(producto.TasaInteresAnualPct,producto.VigenteDesde,producto.VigenteHasta);
        return CreditoFrancesCalculator.Calculate(input.MontoSolicitado,input.PlazoMeses,producto.TasaInteresAnualPct);
    }
    public static void RequireTasaVigente(decimal? tasaInteresAnualPct,DateOnly? vigenteDesde,DateOnly? vigenteHasta)
    {
        if(tasaInteresAnualPct is null) throw new ApplicationError(422,"PRODUCTO_SIN_TASA","El producto no tiene una tasa de interés configurada.");
        var hoy=DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        if((vigenteDesde is {} desde&&hoy<desde)||(vigenteHasta is {} hasta&&hoy>hasta))
            throw new ApplicationError(422,"TASA_NO_VIGENTE","La tasa configurada para este producto no está vigente.");
    }
    public static void Validate(ProductoInput input)
    {
        if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true)
            || input.MontoMinimo<0 || input.MontoMaximo<0 || input.MontoMinimo>input.MontoMaximo
            || input.PlazoMinimoMeses<1 || input.PlazoMaximoMeses<1 || input.PlazoMinimoMeses>input.PlazoMaximoMeses
            || input.TasaInteresAnualPct is null || input.TasaInteresAnualPct<0
            || (input.TipoTasa is not null && !TiposTasa.Validos.Contains(input.TipoTasa))
            || (input.Categoria is not null && !CategoriasProducto.Validas.Contains(input.Categoria))
            || (input.VigenteDesde is not null && input.VigenteHasta is not null && input.VigenteHasta<input.VigenteDesde))
            throw new ApplicationError(400,"VALIDATION","Datos de producto, tasa de interés o límites de monto/plazo/vigencia inválidos.");
    }
}
