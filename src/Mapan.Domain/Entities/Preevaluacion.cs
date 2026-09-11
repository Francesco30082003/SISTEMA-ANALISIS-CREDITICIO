namespace Mapan.Domain.Entities;

public sealed class Preevaluacion
{
    public Guid PreevaluacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid PoliticaVersionId { get; set; }
    public required string ResultadoPreliminar { get; set; }
    public string? SeveridadMaxima { get; set; }
    public string? DetalleJson { get; set; }
    public Guid EjecutadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaEjecucion { get; set; }
}
