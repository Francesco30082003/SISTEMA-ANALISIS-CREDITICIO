namespace Mapan.Domain.Entities;

public sealed class Obligacion
{
    public Guid ObligacionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public string? Institucion { get; set; }
    public string? TipoObligacion { get; set; }
    public string? NumeroOperacionMascara { get; set; }
    public decimal? MontoOriginal { get; set; }
    public decimal? SaldoActual { get; set; }
    public decimal? CuotaMensual { get; set; }
    public int DiasMoraActual { get; set; }
    public int MaxDiasMoraHistorico { get; set; }
    public string? Estado { get; set; }
    public bool EsGarante { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
