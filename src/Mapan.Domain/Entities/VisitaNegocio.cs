namespace Mapan.Domain.Entities;

public sealed class VisitaNegocio
{
    public Guid VisitaNegocioId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public DateOnly FechaVisita { get; set; }
    public string? DireccionObservada { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public bool NegocioExiste { get; set; }
    public string? TipoNegocioObservado { get; set; }
    public string? TiempoFuncionamientoObservado { get; set; }
    public int? NumeroEmpleadosObservado { get; set; }
    public decimal? InventarioEstimado { get; set; }
    public decimal? IngresoMensualEstimadoObservado { get; set; }
    public string? Observaciones { get; set; }
    public required string Recomendacion { get; set; }
    public Guid RealizadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
