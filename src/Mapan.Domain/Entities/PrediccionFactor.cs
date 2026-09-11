namespace Mapan.Domain.Entities;

public sealed class PrediccionFactor
{
    public Guid PrediccionFactorId { get; set; }
    public Guid PrediccionId { get; set; }
    public required string CodigoCaracteristica { get; set; }
    public string? NombreCaracteristica { get; set; }
    public decimal? ValorNumerico { get; set; }
    public string? ValorTexto { get; set; }
    public decimal Contribucion { get; set; }
    public required string Direccion { get; set; }
    public int? PosicionImportancia { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
