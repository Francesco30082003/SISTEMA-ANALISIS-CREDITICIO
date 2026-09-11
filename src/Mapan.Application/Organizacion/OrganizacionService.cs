using Mapan.Application.Security;

namespace Mapan.Application.Organizacion;
public sealed record SucursalDto(Guid SucursalId,string Codigo,string Nombre,string Estado);
public interface IOrganizacionRepository
{
    Task<IReadOnlyList<SucursalDto>> GetSucursalesAsync(Guid empresaId,CancellationToken ct);
    Task SetDefaultBranchAsync(Guid empresaId,Guid membershipId,Guid sucursalId,CancellationToken ct);
}
public sealed class OrganizacionService(IOrganizacionRepository repository,ICurrentTenant tenant)
{
    public Task<IReadOnlyList<SucursalDto>> GetSucursalesAsync(CancellationToken ct)=>repository.GetSucursalesAsync(tenant.EmpresaId,ct);
    public Task SetDefaultBranchAsync(Guid sucursalId,CancellationToken ct)=>repository.SetDefaultBranchAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,sucursalId,ct);
}
