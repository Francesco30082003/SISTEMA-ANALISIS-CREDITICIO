namespace Mapan.Application.Empresas;

public sealed class EmpresaService(IEmpresaRepository repository)
{
    public async Task<IReadOnlyList<EmpresaDto>> ObtenerEmpresasAsync(
        CancellationToken cancellationToken)
    {
        return await repository.ObtenerEmpresasAsync(cancellationToken);
    }
}
