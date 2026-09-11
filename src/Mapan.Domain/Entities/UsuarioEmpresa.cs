namespace Mapan.Domain.Entities;

public sealed class UsuarioEmpresa
{
    public Guid UsuarioEmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? SucursalPredeterminadaId { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaIngreso { get; set; }
}
