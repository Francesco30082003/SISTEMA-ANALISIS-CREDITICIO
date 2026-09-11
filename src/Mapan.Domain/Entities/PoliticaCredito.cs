namespace Mapan.Domain.Entities;

public sealed class PoliticaCredito
{
    public Guid PoliticaCreditoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ProductoCreditoId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
