namespace Mapan.Domain.Entities;

public sealed class IngresoPeriodo
{
    public Guid IngresoPeriodoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid FuenteIngresoId { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFin { get; set; }
    public decimal? MontoBruto { get; set; }
    public decimal MontoNeto { get; set; }
    public string? OrigenDato { get; set; }
    public string? Observacion { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
