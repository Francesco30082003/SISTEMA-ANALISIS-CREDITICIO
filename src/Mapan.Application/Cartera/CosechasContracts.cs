using Mapan.Application.Security;

namespace Mapan.Application.Cartera;

// Un punto de la curva de una cosecha en un MOB (meses desde el desembolso) dado: cuántos préstamos de
// esa cosecha seguían activos en ese mes y qué proporción ya estaba en mora 30+.
public sealed record CosechaPuntoDto(int Mob,int PrestamosActivos,int PrestamosEnMora30,decimal SaldoActivo,decimal SaldoEnMora30,decimal? TasaMora30Pct);
public sealed record CosechaDto(string Cosecha,int PrestamosOriginados,decimal MontoOriginado,IReadOnlyList<CosechaPuntoDto> Puntos);

public interface ICosechaRepository
{
    Task<IReadOnlyList<CosechaDto>> ListAsync(Guid empresaId,Guid? productoCreditoId,Guid? sucursalId,CancellationToken ct);
}

public sealed class CosechaService(ICosechaRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<CosechaDto>> ListAsync(Guid? productoCreditoId,Guid? sucursalId,CancellationToken ct)
    {permissions.Require("Cosechas:Read");return repository.ListAsync(tenant.EmpresaId,productoCreditoId,sucursalId,ct);}
}
