namespace Mapan.Domain.Entities;

public sealed class Prestamo
{
    public Guid PrestamoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public required string NumeroPrestamo { get; set; }
    public decimal MontoDesembolsado { get; set; }
    public DateOnly FechaDesembolso { get; set; }
    public int PlazoMeses { get; set; }
    public decimal? TasaInteresAnualPct { get; set; }
    public decimal? CuotaPactada { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
