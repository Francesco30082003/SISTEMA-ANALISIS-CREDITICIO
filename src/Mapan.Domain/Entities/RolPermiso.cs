namespace Mapan.Domain.Entities;

public sealed class RolPermiso
{
    public Guid RolId { get; set; }
    public Guid PermisoId { get; set; }
    public DateTimeOffset FechaAsignacion { get; set; }
}
