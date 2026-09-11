namespace Mapan.Domain.Entities;

public sealed class Alerta
{
    public Guid AlertaId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AnalisisId { get; set; }
    public Guid? ReglaId { get; set; }
    public required string Codigo { get; set; }
    public required string Nivel { get; set; }
    public required string Titulo { get; set; }
    public required string Descripcion { get; set; }
    public bool Resuelta { get; set; }
    public Guid? ResueltaPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset? FechaResolucion { get; set; }
}
