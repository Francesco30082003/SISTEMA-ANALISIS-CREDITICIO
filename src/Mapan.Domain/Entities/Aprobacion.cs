namespace Mapan.Domain.Entities;

public sealed class Aprobacion
{
    public Guid AprobacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid RecomendacionId { get; set; }
    public Guid RutaAprobacionId { get; set; }
    public required string Estado { get; set; }
    public Guid? CreadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
}
