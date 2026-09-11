namespace Mapan.Domain.Entities;

public sealed class Cliente
{
    public Guid ClienteId { get; set; }
    public Guid EmpresaId { get; set; }
    public required string TipoPersona { get; set; }
    public required string TipoIdentificacion { get; set; }
    public required string NumeroIdentificacion { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? RazonSocial { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }
}
