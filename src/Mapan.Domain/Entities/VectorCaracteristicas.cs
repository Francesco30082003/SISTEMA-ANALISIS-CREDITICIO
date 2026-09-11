namespace Mapan.Domain.Entities;

public sealed class VectorCaracteristicas
{
    public Guid VectorCaracteristicasId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AnalisisId { get; set; }
    public decimal? IngresoMensual { get; set; }
    public decimal? GastosMensuales { get; set; }
    public decimal? CuotasOtrasDeudas { get; set; }
    public decimal? DeudaTotalActual { get; set; }
    public decimal? MontoSolicitado { get; set; }
    public int? PlazoMeses { get; set; }
    public decimal? CuotaEstimada { get; set; }
    public int? MaxDiasMoraHistorico { get; set; }
    public int? CreditosActivos { get; set; }
    public int? AntiguedadActividadMeses { get; set; }
    public decimal? EstabilidadIngresosScore { get; set; }
    public decimal? HistorialInternoScore { get; set; }
    public decimal? IngresoDisponible { get; set; }
    public decimal? CapacidadNuevaCuota { get; set; }
    public decimal? DeudaSobreIngreso { get; set; }
    public decimal? CuotaSobreIngreso { get; set; }
    public decimal? MontoSobreIngreso { get; set; }
    public string? VariablesAdicionales { get; set; }
    // Señales de investigación/verificación/visita (Fases 3, 6 y 7) al momento de este análisis — se
    // copian aquí (no se leen "en vivo" desde sus tablas de origen) para que el vector, ya inmutable,
    // siga reflejando exactamente lo que se usó en esta ejecución aunque el buró se vuelva a consultar después.
    public int? ScoreBuro { get; set; }
    public int? MoraActualMaxDiasBuro { get; set; }
    public bool? TieneProcesosJudiciales { get; set; }
    public string? GravedadJudicial { get; set; }
    public int? DocumentosSospechosos { get; set; }
    public string? VisitaRecomendacion { get; set; }
    public DateTimeOffset FechaSnapshot { get; set; }
}
