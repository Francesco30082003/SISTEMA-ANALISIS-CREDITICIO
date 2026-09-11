namespace Mapan.Domain.Entities;

public sealed class Prediccion
{
    public Guid PrediccionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AnalisisId { get; set; }
    public Guid ModeloVersionId { get; set; }
    public decimal ProbabilidadIncumplimiento { get; set; }
    public decimal UmbralUtilizado { get; set; }
    public required string ClasePredicha { get; set; }
    public string? NivelRiesgo { get; set; }
    public int? TiempoInferenciaMs { get; set; }
    public DateTimeOffset FechaPrediccion { get; set; }
}
