namespace Mapan.Domain.Entities;

public sealed class Gasto
{
    public Guid GastoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public required string TipoGasto { get; set; }
    public string? Descripcion { get; set; }
    public decimal MontoMensual { get; set; }
    public string? OrigenDato { get; set; }
    public bool Declarado { get; set; }
    public bool Verificado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
