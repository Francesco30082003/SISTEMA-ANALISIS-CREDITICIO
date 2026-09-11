namespace Mapan.Domain.Entities;

public sealed class Documento
{
    public Guid DocumentoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public required string TipoDocumento { get; set; }
    public required string Nombre { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Emisor { get; set; }
    public DateOnly? FechaEmision { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public required string Estado { get; set; }
    public string EstadoVerificacion { get; set; } = "PENDIENTE";
    public Guid? CreadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
