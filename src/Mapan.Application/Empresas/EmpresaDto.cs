namespace Mapan.Application.Empresas;

public sealed record EmpresaDto(
    Guid EmpresaId,
    string Codigo,
    string NombreLegal,
    string? NombreComercial,
    string PaisCodigo,
    string MonedaCodigo,
    string Estado);
