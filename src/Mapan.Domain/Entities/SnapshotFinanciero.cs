namespace Mapan.Domain.Entities;

public sealed class SnapshotFinanciero
{
    public Guid SnapshotFinancieroId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AnalisisId { get; set; }
    public decimal IngresoTotalMensual { get; set; }
    public decimal GastoTotalMensual { get; set; }
    public decimal IngresoDisponible { get; set; }
    public decimal FactorCapacidad { get; set; }
    public decimal CapacidadNuevaCuota { get; set; }
    public decimal DeudaTotalActual { get; set; }
    public decimal CuotasActuales { get; set; }
    public decimal CuotaNuevaEstimada { get; set; }
    public decimal? RatioEndeudamientoActual { get; set; }
    public decimal? RatioEndeudamientoPost { get; set; }
    public bool CuotaCompatible { get; set; }
    public DateTimeOffset FechaCalculo { get; set; }
}
