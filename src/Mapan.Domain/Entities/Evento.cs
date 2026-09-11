namespace Mapan.Domain.Entities;

public sealed class Evento
{
    public Guid EventoId { get; set; }
    public Guid? EmpresaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? UsuarioEmpresaId { get; set; }
    public string? CorrelationId { get; set; }
    public required string Origen { get; set; }
    public required string EntidadTipo { get; set; }
    public string? EntidadId { get; set; }
    public required string Accion { get; set; }
    public string? DatosAnteriores { get; set; }
    public string? DatosNuevos { get; set; }
    public string? DetalleJson { get; set; }
    public bool Exitoso { get; set; }
    public System.Net.IPAddress? DireccionIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset FechaEvento { get; set; }
}
