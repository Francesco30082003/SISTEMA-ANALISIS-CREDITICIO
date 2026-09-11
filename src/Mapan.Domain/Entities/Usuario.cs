namespace Mapan.Domain.Entities;

public sealed class Usuario
{
    public Guid UsuarioId { get; set; }
    public required string NombreUsuario { get; set; }
    public required string Correo { get; set; }
    public required string PasswordHash { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public bool MfaHabilitado { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
    public DateTimeOffset? UltimoAcceso { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }
}
