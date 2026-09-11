namespace Mapan.Domain.Entities;

public sealed class EmpresaProveedor
{
    public Guid EmpresaProveedorId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ProveedorId { get; set; }
    public required string Ambiente { get; set; }
    public string? ConfiguracionJson { get; set; }
    public string? SecretoRef { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
