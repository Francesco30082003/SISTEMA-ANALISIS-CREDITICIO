namespace Mapan.Domain.Entities;

public sealed class Regla
{
    public Guid ReglaId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PoliticaVersionId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
    public int Prioridad { get; set; }
    public required string Severidad { get; set; }
    public required string Etapa { get; set; }
    public required string CondicionJson { get; set; }
    public required string AccionJson { get; set; }
    public bool Activa { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
