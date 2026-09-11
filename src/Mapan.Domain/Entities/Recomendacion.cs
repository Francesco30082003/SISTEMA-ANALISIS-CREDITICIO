namespace Mapan.Domain.Entities;

public sealed class Recomendacion
{
    public Guid RecomendacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AnalisisId { get; set; }
    public Guid? PrediccionId { get; set; }
    public required string CodigoRecomendacion { get; set; }
    public string? NivelRiesgoFinal { get; set; }
    public bool? CumpleCapacidad { get; set; }
    public string? SeveridadMaximaReglas { get; set; }
    public decimal? ProbabilidadIncumplimiento { get; set; }
    public bool RequiereRevisionHumana { get; set; }
    public string? Resumen { get; set; }
    public string? DetalleJson { get; set; }
    public DateTimeOffset FechaGeneracion { get; set; }
}
