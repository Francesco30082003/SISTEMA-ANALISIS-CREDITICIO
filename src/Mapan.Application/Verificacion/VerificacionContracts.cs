using Mapan.Application.Security;

namespace Mapan.Application.Verificacion;

public sealed record DocumentoVerificacionDto(Guid SolicitudDocumentoId,Guid DocumentoId,string Nombre,string TipoDocumento,
    string EstadoVerificacion,decimal? ConfianzaPct,IReadOnlyList<string>? Motivos,DateTimeOffset? FechaVerificacion);
public sealed record VerificacionLaboralDto(bool? RelacionLaboralActiva,string? EmpleadorRegistrado,DateOnly? FechaAfiliacion,
    decimal? AporteMensual,string? Estado,DateTimeOffset? UltimaEjecucion);

public interface IVerificacionRepository
{
    Task<IReadOnlyList<DocumentoVerificacionDto>> ListarDocumentosAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<DocumentoVerificacionDto> VerificarDocumentoAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,CancellationToken ct);
    Task<VerificacionLaboralDto?> GetUltimaLaboralAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<VerificacionLaboralDto> EjecutarLaboralAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct);
}

public sealed class VerificacionService(IVerificacionRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<DocumentoVerificacionDto>> ListarDocumentosAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Verificacion:Read");return repository.ListarDocumentosAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<DocumentoVerificacionDto> VerificarDocumentoAsync(Guid solicitudDocumentoId,CancellationToken ct)
    {permissions.Require("Verificacion:Execute");return repository.VerificarDocumentoAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudDocumentoId,ct);}
    public Task<VerificacionLaboralDto?> GetUltimaLaboralAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Verificacion:Read");return repository.GetUltimaLaboralAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<VerificacionLaboralDto> EjecutarLaboralAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Verificacion:Execute");return repository.EjecutarLaboralAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudId,ct);}
}
