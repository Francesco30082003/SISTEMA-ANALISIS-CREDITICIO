namespace Mapan.Domain.Entities;

public sealed class ModeloVersion
{
    public Guid ModeloVersionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ModeloId { get; set; }
    public int NumeroVersion { get; set; }
    public required string Algoritmo { get; set; }
    public string? Descripcion { get; set; }
    public string? ArtefactoUri { get; set; }
    public required string EsquemaCaracteristicas { get; set; }
    public string? Hiperparametros { get; set; }
    public DateOnly? FechaDatosDesde { get; set; }
    public DateOnly? FechaDatosHasta { get; set; }
    public int? CantidadRegistros { get; set; }
    public int? CantidadPositivos { get; set; }
    public int? CantidadNegativos { get; set; }
    public decimal? RocAuc { get; set; }
    public decimal? PrecisionScore { get; set; }
    public decimal? RecallScore { get; set; }
    public decimal? F1Score { get; set; }
    public decimal? AccuracyScore { get; set; }
    public decimal? UmbralDecision { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset? FechaEntrenamiento { get; set; }
    public DateTimeOffset? FechaValidacion { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
