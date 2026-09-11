namespace Mapan.Domain.Entities;

public sealed class Rol
{
    public Guid RolId { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
