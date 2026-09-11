namespace Mapan.Domain.Entities;

public sealed class DesempenoCredito
{
    public Guid DesempenoCreditoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PrestamoId { get; set; }
    public DateOnly FechaCorte { get; set; }
    public decimal SaldoCapital { get; set; }
    public decimal CuotaExigible { get; set; }
    public decimal MontoPagadoPeriodo { get; set; }
    public int DiasMora { get; set; }
    public bool? Mora15 { get; set; }
    public bool? Mora30 { get; set; }
    public bool? Mora60 { get; set; }
    public bool? Mora90 { get; set; }
    public bool Refinanciado { get; set; }
    public bool Reestructurado { get; set; }
    public bool Castigado { get; set; }
    public string? EstadoCartera { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
