using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;

namespace Mapan.Application.Clientes;

public sealed record ClienteInput(
    [Required, RegularExpression("NATURAL|JURIDICA")] string TipoPersona,
    [Required, MaxLength(20)] string TipoIdentificacion,
    [Required, MaxLength(30)] string NumeroIdentificacion,
    [MaxLength(150)] string? Nombres, [MaxLength(150)] string? Apellidos,
    [MaxLength(200)] string? RazonSocial, DateOnly? FechaNacimiento,
    [MaxLength(30)] string? Telefono, [MaxLength(200), EmailAddress] string? Correo);
public sealed record ClienteDto(Guid ClienteId, string TipoPersona, string TipoIdentificacion,
    string NumeroIdentificacion, string? Nombres, string? Apellidos, string? RazonSocial,
    DateOnly? FechaNacimiento, string? Telefono, string? Correo, string Estado);
public interface IClienteRepository
{
    Task<Page<ClienteDto>> ListAsync(Guid empresaId, string? search, int page, int size, CancellationToken ct);
    Task<ClienteDto?> GetAsync(Guid empresaId, Guid id, CancellationToken ct);
    Task<ClienteDto> CreateAsync(Guid empresaId, ClienteInput input, CancellationToken ct);
    Task<ClienteDto> UpdateAsync(Guid empresaId, Guid id, ClienteInput input, CancellationToken ct);
    Task InactivateAsync(Guid empresaId, Guid id, CancellationToken ct);
}
