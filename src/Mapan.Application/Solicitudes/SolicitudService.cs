using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Productos;
using Mapan.Application.Security;
using Mapan.Domain.Credito;

namespace Mapan.Application.Solicitudes;
public sealed record SolicitudInput(Guid SucursalId,Guid ClienteId,Guid ProductoCreditoId,
    [Required,MaxLength(50)] string NumeroSolicitud,decimal MontoSolicitado,int PlazoSolicitadoMeses,
    decimal? TasaInteresAnualPct,decimal? CapitalMensualEstimado,decimal? InteresMensualEstimado,decimal? CuotaEstimada,
    decimal? InteresTotalEstimado,decimal? TotalAPagarEstimado,[MaxLength(500)] string? DestinoCredito);
public sealed record SolicitudDto(Guid SolicitudCreditoId,Guid SucursalId,Guid ClienteId,Guid ProductoCreditoId,
    string NumeroSolicitud,decimal MontoSolicitado,int PlazoSolicitadoMeses,decimal? TasaInteresAnualPct,
    decimal? CapitalMensualEstimado,decimal? InteresMensualEstimado,decimal? CuotaEstimada,decimal? InteresTotalEstimado,
    decimal? TotalAPagarEstimado,string? DestinoCredito,string Estado,DateTimeOffset FechaCreacion);
public interface ISolicitudRepository
{
    Task<Page<SolicitudDto>> ListAsync(Guid empresaId,Guid? clienteId,int page,int size,CancellationToken ct);
    Task<SolicitudDto?> GetAsync(Guid empresaId,Guid id,CancellationToken ct);
    Task<SolicitudDto> CreateAsync(Guid empresaId,Guid membershipId,SolicitudInput input,CancellationToken ct);
    Task<SolicitudDto> UpdateDraftAsync(Guid empresaId,Guid id,SolicitudInput input,CancellationToken ct);
    Task SendToDocumentationAsync(Guid empresaId,Guid id,CancellationToken ct);
}
public sealed class SolicitudService(ISolicitudRepository repository,IProductoRepository productos,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<Page<SolicitudDto>> ListAsync(Guid? clienteId,int page,int size,CancellationToken ct) {permissions.Require("Solicitudes:Read");Paging.Validate(page,size);return repository.ListAsync(tenant.EmpresaId,clienteId,page,size,ct);}
    public async Task<SolicitudDto> GetAsync(Guid id,CancellationToken ct) {permissions.Require("Solicitudes:Read");return await repository.GetAsync(tenant.EmpresaId,id,ct)??throw ApplicationError.NotFound();}
    public async Task<SolicitudDto> CreateAsync(SolicitudInput input,CancellationToken ct) {permissions.Require("Solicitudes:Create");Validate(input);return await repository.CreateAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,await WithComputedCuotaAsync(input,ct),ct);}
    public async Task<SolicitudDto> UpdateDraftAsync(Guid id,SolicitudInput input,CancellationToken ct) {permissions.Require("Solicitudes:Update");Validate(input);return await repository.UpdateDraftAsync(tenant.EmpresaId,id,await WithComputedCuotaAsync(input,ct),ct);}
    public Task SendToDocumentationAsync(Guid id,CancellationToken ct) {permissions.Require("Solicitudes:Send");return repository.SendToDocumentationAsync(tenant.EmpresaId,id,ct);}
    private static void Validate(SolicitudInput input)
    {
        if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true)||input.SucursalId==Guid.Empty
            ||input.ClienteId==Guid.Empty||input.ProductoCreditoId==Guid.Empty||input.MontoSolicitado<=0
            ||input.PlazoSolicitadoMeses<=0)
            throw new ApplicationError(400,"VALIDATION","Datos de solicitud inválidos.");
    }
    // La tasa y el desglose financiero nunca vienen del cliente: se derivan de la tasa vigente del producto
    // y se recalculan con CreditoFrancesCalculator, para que quede un snapshot fiel al momento de la solicitud
    // aunque el producto cambie su tasa después (ver §2/§31 del pedido de negocio).
    private async Task<SolicitudInput> WithComputedCuotaAsync(SolicitudInput input,CancellationToken ct)
    {
        var producto=await productos.GetAsync(tenant.EmpresaId,input.ProductoCreditoId,ct)??throw ApplicationError.NotFound();
        ProductoService.RequireTasaVigente(producto.TasaInteresAnualPct,producto.VigenteDesde,producto.VigenteHasta);
        var calculo=CreditoFrancesCalculator.Calculate(input.MontoSolicitado,input.PlazoSolicitadoMeses,producto.TasaInteresAnualPct);
        return input with {
            TasaInteresAnualPct=producto.TasaInteresAnualPct,
            CapitalMensualEstimado=calculo.CapitalMensual,
            InteresMensualEstimado=calculo.InteresMensual,
            CuotaEstimada=calculo.CuotaEstimada,
            InteresTotalEstimado=calculo.InteresTotal,
            TotalAPagarEstimado=calculo.TotalAPagar,
        };
    }
}
