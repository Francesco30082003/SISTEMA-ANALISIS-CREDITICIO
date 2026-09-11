namespace Mapan.Domain.Entities;

public sealed class SolicitudDocumento
{
    public Guid SolicitudDocumentoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid DocumentoVersionId { get; set; }
    public string? UsoDocumento { get; set; }
    public bool Obligatorio { get; set; }
    public required string Estado { get; set; }
    public Guid? AsociadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaAsociacion { get; set; }
}
