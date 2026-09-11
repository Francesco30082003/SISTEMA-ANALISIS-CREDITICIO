namespace Mapan.Domain.Entities;

public sealed class ActividadEconomica
{
    public Guid ActividadEconomicaId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid SolicitudCreditoId { get; set; }
    public required string TipoActividad { get; set; }
    public string? EmpleadorNegocio { get; set; }
    public string? CargoActividad { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public string? Ruc { get; set; }
    public string? Descripcion { get; set; }
    public bool EsPrincipal { get; set; }
    public bool Verificada { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
