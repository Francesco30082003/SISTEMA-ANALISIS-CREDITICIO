namespace Mapan.Domain.Entities;

public sealed class DecisionPaso
{
    public Guid DecisionPasoId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid AprobacionPasoId { get; set; }
    public Guid UsuarioEmpresaId { get; set; }
    public required string Decision { get; set; }
    public string? Comentario { get; set; }
    public DateTimeOffset FechaDecision { get; set; }
}
