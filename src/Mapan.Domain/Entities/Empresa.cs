namespace Mapan.Domain.Entities;

public sealed class Empresa
{
    public Guid EmpresaId { get; set; }
    public required string Codigo { get; set; }
    public required string NombreLegal { get; set; }
    public string? NombreComercial { get; set; }
    public string? TipoIdentificacionFiscal { get; set; }
    public string? IdentificacionFiscal { get; set; }
    public required string PaisCodigo { get; set; }
    public required string MonedaCodigo { get; set; }
    public required string Estado { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaActualizacion { get; set; }
}
