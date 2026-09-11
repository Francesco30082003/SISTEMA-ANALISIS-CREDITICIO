using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Visitas;

public sealed record VisitaNegocioInput(DateOnly FechaVisita,string? DireccionObservada,decimal? Latitud,decimal? Longitud,
    bool NegocioExiste,string? TipoNegocioObservado,string? TiempoFuncionamientoObservado,int? NumeroEmpleadosObservado,
    decimal? InventarioEstimado,decimal? IngresoMensualEstimadoObservado,string? Observaciones,
    [Required,RegularExpression("^(FAVORABLE_CON_OBSERVACIONES|FAVORABLE|DESFAVORABLE)$")] string Recomendacion);

public sealed record VisitaNegocioDto(Guid VisitaNegocioId,Guid SolicitudCreditoId,DateOnly FechaVisita,string? DireccionObservada,
    decimal? Latitud,decimal? Longitud,bool NegocioExiste,string? TipoNegocioObservado,string? TiempoFuncionamientoObservado,
    int? NumeroEmpleadosObservado,decimal? InventarioEstimado,decimal? IngresoMensualEstimadoObservado,string? Observaciones,
    string Recomendacion,Guid RealizadaPorUsuarioEmpresaId,string RealizadaPorNombre,DateTimeOffset FechaCreacion);

public interface IVisitaNegocioRepository
{
    Task<IReadOnlyList<VisitaNegocioDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<Guid> RegistrarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,VisitaNegocioInput input,CancellationToken ct);
}

public sealed class VisitaNegocioService(IVisitaNegocioRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<VisitaNegocioDto>> ListAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("VisitaNegocio:Read");return repository.ListAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<Guid> RegistrarAsync(Guid solicitudId,VisitaNegocioInput input,CancellationToken ct)
    {
        permissions.Require("VisitaNegocio:Create");
        if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true))throw new ApplicationError(400,"VALIDATION","Datos de visita inválidos.");
        return repository.RegistrarAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudId,input,ct);
    }
}
