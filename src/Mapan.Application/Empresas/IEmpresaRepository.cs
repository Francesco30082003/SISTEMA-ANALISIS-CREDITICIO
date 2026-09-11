namespace Mapan.Application.Empresas;

public interface IEmpresaRepository
{
    Task<IReadOnlyList<EmpresaDto>> ObtenerEmpresasAsync(CancellationToken cancellationToken);
}
