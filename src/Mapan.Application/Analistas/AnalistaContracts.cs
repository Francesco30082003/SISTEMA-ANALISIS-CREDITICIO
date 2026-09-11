using Mapan.Application.Security;

namespace Mapan.Application.Analistas;

public sealed record AnalistaDesempenoDto(Guid UsuarioEmpresaId, string Analista, Guid SucursalId, string Sucursal,
    int SolicitudesTotal, int SolicitudesAprobadas, int SolicitudesRechazadas, int SolicitudesEnProceso,
    decimal MontoColocado, decimal? TasaAprobacionPct, int PrestamosEnMora, decimal SaldoEnMora);

public interface IAnalistaDesempenoRepository
{
    Task<IReadOnlyList<AnalistaDesempenoDto>> ListAsync(Guid empresaId, Guid member, CancellationToken ct);
}

public sealed class AnalistaDesempenoService(IAnalistaDesempenoRepository repository, ICurrentTenant tenant, IPermissionChecker permissions)
{
    public Task<IReadOnlyList<AnalistaDesempenoDto>> ListAsync(CancellationToken ct)
    {
        permissions.Require("Analistas:Read");
        return repository.ListAsync(tenant.EmpresaId, tenant.UsuarioEmpresaId, ct);
    }
}
