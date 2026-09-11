namespace Mapan.Domain.Entities;

public sealed class ProductoCredito
{
    public Guid ProductoCreditoId { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string Categoria { get; set; } = "OTRO";
    public string? Descripcion { get; set; }
    public decimal? MontoMinimo { get; set; }
    public decimal? MontoMaximo { get; set; }
    public int? PlazoMinimoMeses { get; set; }
    public int? PlazoMaximoMeses { get; set; }
    public required string MonedaCodigo { get; set; }
    public decimal? TasaInteresAnualPct { get; set; }
    public string? TipoTasa { get; set; }
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
