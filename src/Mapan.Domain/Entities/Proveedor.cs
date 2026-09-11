namespace Mapan.Domain.Entities;

public sealed class Proveedor
{
    public Guid ProveedorId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required string Tipo { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
