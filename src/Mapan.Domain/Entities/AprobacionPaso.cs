namespace Mapan.Domain.Entities;

public sealed class AprobacionPaso
{
    public Guid AprobacionPasoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AprobacionId { get; set; }
    public Guid RutaAprobacionPasoId { get; set; }
    public int Orden { get; set; }
    public required string NombrePaso { get; set; }
    public Guid RolId { get; set; }
    public int CantidadAprobacionesRequeridas { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset? FechaHabilitacion { get; set; }
    public DateTimeOffset? FechaCompletado { get; set; }
}
