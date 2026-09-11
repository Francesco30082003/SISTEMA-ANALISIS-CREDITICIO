namespace Mapan.Domain.Entities;

public sealed class Permiso
{
    public Guid PermisoId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
}
