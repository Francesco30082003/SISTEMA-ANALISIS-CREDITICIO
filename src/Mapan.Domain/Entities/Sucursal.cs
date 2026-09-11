namespace Mapan.Domain.Entities;

public sealed class Sucursal
{
    public Guid SucursalId { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required string PaisCodigo { get; set; }
    public string? Provincia { get; set; }
    public string? Ciudad { get; set; }
    public string? Direccion { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
