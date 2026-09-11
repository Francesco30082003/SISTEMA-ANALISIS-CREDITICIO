namespace Mapan.Domain.Entities;

public sealed class ReglaEnrutamiento
{
    public Guid ReglaEnrutamientoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid RutaAprobacionId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Prioridad { get; set; }
    public required string CondicionJson { get; set; }
    public bool Activa { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
