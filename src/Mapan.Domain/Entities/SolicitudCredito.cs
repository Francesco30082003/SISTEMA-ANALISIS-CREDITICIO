namespace Mapan.Domain.Entities;

public sealed class SolicitudCredito
{
    public Guid SolicitudCreditoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SucursalId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid ProductoCreditoId { get; set; }
    public required string NumeroSolicitud { get; set; }
    public decimal MontoSolicitado { get; set; }
    public int PlazoSolicitadoMeses { get; set; }
    public decimal? TasaInteresAnualPct { get; set; }
    public decimal? CapitalMensualEstimado { get; set; }
    public decimal? InteresMensualEstimado { get; set; }
    public decimal? CuotaEstimada { get; set; }
    public decimal? InteresTotalEstimado { get; set; }
    public decimal? TotalAPagarEstimado { get; set; }
    public string? DestinoCredito { get; set; }
    public required string Estado { get; set; }
    public Guid CreadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaEnvio { get; set; }
    public DateTimeOffset? FechaFinalizacion { get; set; }
}
