namespace Mapan.Domain.Entities;

public sealed class DatoExtraido
{
    public Guid DatoExtraidoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid DocumentoVersionId { get; set; }
    public required string CodigoCampo { get; set; }
    public string? NombreCampo { get; set; }
    public int IndiceOcurrencia { get; set; }
    public required string TipoDato { get; set; }
    public string? ValorOriginal { get; set; }
    public string? ValorTexto { get; set; }
    public decimal? ValorNumerico { get; set; }
    public DateOnly? ValorFecha { get; set; }
    public bool? ValorBooleano { get; set; }
    public string? ValorJson { get; set; }
    public int? Pagina { get; set; }
    public decimal? Confianza { get; set; }
    public string? ModeloExtraccion { get; set; }
    public string? VersionExtractor { get; set; }
    public string? UbicacionJson { get; set; }
    public DateTimeOffset FechaExtraccion { get; set; }
}
