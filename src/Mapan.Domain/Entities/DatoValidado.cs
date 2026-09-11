namespace Mapan.Domain.Entities;

public sealed class DatoValidado
{
    public Guid DatoValidadoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudDocumentoId { get; set; }
    public Guid? DatoExtraidoId { get; set; }
    public required string CodigoCampo { get; set; }
    public int NumeroRevision { get; set; }
    public required string TipoDato { get; set; }
    public string? ValorTexto { get; set; }
    public decimal? ValorNumerico { get; set; }
    public DateOnly? ValorFecha { get; set; }
    public bool? ValorBooleano { get; set; }
    public string? ValorJson { get; set; }
    public required string Estado { get; set; }
    public string? Comentario { get; set; }
    public Guid ValidadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaValidacion { get; set; }
}
