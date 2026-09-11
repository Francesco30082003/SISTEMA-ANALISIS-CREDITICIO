namespace Mapan.Domain.Entities;

public sealed class PoliticaVersion
{
    public Guid PoliticaVersionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PoliticaCreditoId { get; set; }
    public int NumeroVersion { get; set; }
    public DateTimeOffset VigenteDesde { get; set; }
    public DateTimeOffset? VigenteHasta { get; set; }
    public required string Estado { get; set; }
    public Guid CreadaPorUsuarioEmpresaId { get; set; }
    public Guid? AprobadaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaAprobacion { get; set; }
}
