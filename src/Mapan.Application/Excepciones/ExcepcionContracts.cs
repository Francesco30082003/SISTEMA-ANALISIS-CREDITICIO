using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Excepciones;

public sealed record ExcepcionInput(Guid SolicitudCreditoId,Guid? ReglaId,[Required]string MotivoJustificacion,string? Observacion,string? Evidencia);
public sealed record ExcepcionDto(Guid ExcepcionId,Guid SolicitudCreditoId,Guid? ReglaId,string? ReglaNombre,string MotivoJustificacion,
    string? Observacion,string? Evidencia,string Estado,Guid SolicitadaPorUsuarioEmpresaId,string? SolicitadaPorNombre,DateTimeOffset FechaSolicitud,
    Guid? ResueltaPorUsuarioEmpresaId,string? ResueltaPorNombre,DateTimeOffset? FechaResolucion,string? ComentarioResolucion);

public interface IExcepcionRepository
{
    Task<IReadOnlyList<ExcepcionDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<Guid> SolicitarAsync(Guid empresaId,Guid membershipId,ExcepcionInput input,CancellationToken ct);
    Task ResolverAsync(Guid empresaId,Guid membershipId,Guid id,bool aprobar,string? comentario,CancellationToken ct);
}

public sealed class ExcepcionService(IExcepcionRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<ExcepcionDto>> ListAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Excepciones:Read");return repository.ListAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<Guid> SolicitarAsync(ExcepcionInput input,CancellationToken ct)
    {
        permissions.Require("Excepciones:Solicitar");
        if(string.IsNullOrWhiteSpace(input.MotivoJustificacion))throw new ApplicationError(400,"VALIDATION","El motivo de la excepción es obligatorio.");
        return repository.SolicitarAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,input,ct);
    }
    public Task ResolverAsync(Guid id,bool aprobar,string? comentario,CancellationToken ct)
    {permissions.Require("Excepciones:Aprobar");return repository.ResolverAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,aprobar,comentario,ct);}
}
