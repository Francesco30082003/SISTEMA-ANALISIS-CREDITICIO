namespace Mapan.Domain.Entities;

public sealed class RutaAprobacionPaso
{
    public Guid RutaAprobacionPasoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid RutaAprobacionId { get; set; }
    public int Orden { get; set; }
    public required string Nombre { get; set; }
    public Guid RolId { get; set; }
    public bool Obligatorio { get; set; }
    public int CantidadAprobacionesRequeridas { get; set; }
    public bool PermiteAprobar { get; set; }
    public bool PermiteRechazar { get; set; }
    public bool PermiteDevolver { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
