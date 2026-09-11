namespace Mapan.Domain.Entities;

public sealed class IessSnapshot
{
    public Guid IessSnapshotId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid EmpresaProveedorId { get; set; }
    public Guid? ConsultaExternaId { get; set; }
    public bool? RelacionLaboralActiva { get; set; }
    public string? EmpleadorRegistrado { get; set; }
    public DateOnly? FechaAfiliacion { get; set; }
    public decimal? AporteMensual { get; set; }
    public string? Estado { get; set; }
    public string? PayloadNormalizado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
