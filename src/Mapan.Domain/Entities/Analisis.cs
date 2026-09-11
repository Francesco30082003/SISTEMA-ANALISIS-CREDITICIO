namespace Mapan.Domain.Entities;

public sealed class Analisis
{
    public Guid AnalisisId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public Guid PoliticaVersionId { get; set; }
    public int NumeroEjecucion { get; set; }
    public required string TipoAnalisis { get; set; }
    public required string Estado { get; set; }
    public Guid EjecutadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset IniciadoEn { get; set; }
    public DateTimeOffset? FinalizadoEn { get; set; }
}
