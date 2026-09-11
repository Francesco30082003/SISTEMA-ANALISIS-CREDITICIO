using Mapan.Application.Security;

namespace Mapan.Application.Preevaluacion;

public sealed record ReglaEvaluadaDto(string Codigo,string Nombre,string? Descripcion,string Severidad,string Accion,bool Aplica);
public sealed record PreevaluacionResumenDto(Guid SolicitudCreditoId,string ResultadoPreliminar,string? SeveridadMaxima,
    IReadOnlyList<ReglaEvaluadaDto> Reglas,DateTimeOffset FechaEjecucion);

public interface IPreevaluacionRepository
{
    Task<PreevaluacionResumenDto?> GetUltimaAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<PreevaluacionResumenDto> EjecutarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct);
}

public sealed class PreevaluacionService(IPreevaluacionRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<PreevaluacionResumenDto?> GetUltimaAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Preevaluacion:Read");return repository.GetUltimaAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<PreevaluacionResumenDto> EjecutarAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Preevaluacion:Execute");return repository.EjecutarAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudId,ct);}
}
