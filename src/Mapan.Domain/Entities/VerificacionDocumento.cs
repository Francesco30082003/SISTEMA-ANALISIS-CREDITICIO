namespace Mapan.Domain.Entities;

public sealed class VerificacionDocumento
{
    public Guid VerificacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid DocumentoId { get; set; }
    public Guid? EmpresaProveedorId { get; set; }
    public required string Resultado { get; set; }
    public decimal? ConfianzaPct { get; set; }
    public string? MotivosJson { get; set; }
    public string? Observacion { get; set; }
    public Guid? EjecutadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
