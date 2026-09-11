namespace Mapan.Domain.Entities;

public sealed class Excepcion
{
    public Guid ExcepcionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid? ReglaId { get; set; }
    public required string MotivoJustificacion { get; set; }
    public string? Observacion { get; set; }
    public string? Evidencia { get; set; }
    public required string Estado { get; set; }
    public Guid SolicitadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaSolicitud { get; set; }
    public Guid? ResueltaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset? FechaResolucion { get; set; }
    public string? ComentarioResolucion { get; set; }
}
