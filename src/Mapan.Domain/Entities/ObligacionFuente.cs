namespace Mapan.Domain.Entities;

public sealed class ObligacionFuente
{
    public Guid ObligacionFuenteId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ObligacionId { get; set; }
    public required string TipoFuente { get; set; }
    public Guid? BuroSnapshotId { get; set; }
    public Guid? SolicitudDocumentoId { get; set; }
    public string? ReferenciaFuente { get; set; }
    public decimal? SaldoReportado { get; set; }
    public decimal? CuotaReportada { get; set; }
    public int? DiasMoraReportado { get; set; }
    public DateOnly? FechaFuente { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
