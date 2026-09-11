namespace Mapan.Domain.Entities;

public sealed class UsuarioEmpresaRol
{
    public Guid UsuarioEmpresaId { get; set; }
    public Guid RolId { get; set; }
    public DateTimeOffset FechaAsignacion { get; set; }
}
