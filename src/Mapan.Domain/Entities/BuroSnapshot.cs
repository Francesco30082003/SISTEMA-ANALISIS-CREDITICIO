namespace Mapan.Domain.Entities;

public sealed class BuroSnapshot
{
    public Guid BuroSnapshotId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid EmpresaProveedorId { get; set; }
    public Guid? ConsultaExternaId { get; set; }
    public Guid? SolicitudDocumentoId { get; set; }
    public DateOnly? FechaReporte { get; set; }
    public decimal? ScoreBuro { get; set; }
    public decimal? DeudaTotal { get; set; }
    public decimal? CuotaTotal { get; set; }
    public int? CreditosActivos { get; set; }
    public int? MoraActualMaxDias { get; set; }
    public int? MoraHistoricaMaxDias { get; set; }
    public string? PayloadNormalizado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
