namespace Mapan.Domain.Entities;

public sealed class FuenteIngreso
{
    public Guid FuenteIngresoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid? ActividadEconomicaId { get; set; }
    public required string TipoIngreso { get; set; }
    public string? Descripcion { get; set; }
    public required string MonedaCodigo { get; set; }
    public bool EsRecurrente { get; set; }
    public bool Declarado { get; set; }
    public bool Verificado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
