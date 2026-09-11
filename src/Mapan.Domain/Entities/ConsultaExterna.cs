namespace Mapan.Domain.Entities;

public sealed class ConsultaExterna
{
    public Guid ConsultaExternaId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid EmpresaProveedorId { get; set; }
    public required string TipoConsulta { get; set; }
    public required string Estado { get; set; }
    public string? ReferenciaExterna { get; set; }
    public string? SolicitudResumenJson { get; set; }
    public string? RespuestaResumenJson { get; set; }
    public string? ArchivoRespuestaUri { get; set; }
    public string? MensajeError { get; set; }
    public Guid? IniciadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
}
