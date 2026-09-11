namespace Mapan.Domain.Entities;

public sealed class RutaAprobacion
{
    public Guid RutaAprobacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ProductoCreditoId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminada { get; set; }
    public bool Activa { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
