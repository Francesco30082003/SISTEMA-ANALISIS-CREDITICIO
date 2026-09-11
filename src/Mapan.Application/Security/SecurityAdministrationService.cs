using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;

namespace Mapan.Application.Security;
public sealed record UserInput([Required,MaxLength(100)]string NombreUsuario,[Required,EmailAddress,MaxLength(200)]string Correo,
    [Required,MaxLength(120)]string Nombres,[Required,MaxLength(120)]string Apellidos,[Required,MinLength(12),MaxLength(1024)]string Password);
public sealed record UserDto(Guid UsuarioEmpresaId,Guid UsuarioId,string NombreUsuario,string Correo,string Nombres,string Apellidos,string Estado);
public sealed record RoleInput([Required,MaxLength(50)]string Codigo,[Required,MaxLength(100)]string Nombre,[MaxLength(300)]string? Descripcion);
public sealed record RoleDto(Guid RolId,string Codigo,string Nombre,string? Descripcion,string Estado);
public sealed record PermissionDto(Guid PermisoId,string Codigo,string Nombre);
public interface ISecurityAdministrationRepository
{
    Task<IReadOnlyList<Guid>> UserRolesAsync(Guid empresaId,Guid id,CancellationToken ct);
    Task<IReadOnlyList<Guid>> RolePermissionsAsync(Guid empresaId,Guid id,CancellationToken ct);
    Task<IReadOnlyList<UserDto>> UsersAsync(Guid empresaId,CancellationToken ct);
    Task<Guid> CreateUserAsync(Guid empresaId,UserInput input,CancellationToken ct);
    Task<IReadOnlyList<RoleDto>> RolesAsync(Guid empresaId,CancellationToken ct);
    Task<Guid> CreateRoleAsync(Guid empresaId,RoleInput input,CancellationToken ct);
    Task<IReadOnlyList<PermissionDto>> PermissionsAsync(CancellationToken ct);
    Task AssignRolesAsync(Guid empresaId,Guid membershipId,IReadOnlyList<Guid> roleIds,CancellationToken ct);
    Task AssignPermissionsAsync(Guid empresaId,Guid roleId,IReadOnlyList<Guid> permissionIds,CancellationToken ct);
}
public sealed class SecurityAdministrationService(ISecurityAdministrationRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<IReadOnlyList<Guid>> UserRolesAsync(Guid id,CancellationToken ct){Check();return repository.UserRolesAsync(tenant.EmpresaId,id,ct);}
    public Task<IReadOnlyList<Guid>> RolePermissionsAsync(Guid id,CancellationToken ct){Check();return repository.RolePermissionsAsync(tenant.EmpresaId,id,ct);}
    public Task<IReadOnlyList<UserDto>> UsersAsync(CancellationToken ct){Check();return repository.UsersAsync(tenant.EmpresaId,ct);}
    public Task<Guid> CreateUserAsync(UserInput input,CancellationToken ct){Check(input);return repository.CreateUserAsync(tenant.EmpresaId,input,ct);}
    public Task<IReadOnlyList<RoleDto>> RolesAsync(CancellationToken ct){Check();return repository.RolesAsync(tenant.EmpresaId,ct);}
    public Task<Guid> CreateRoleAsync(RoleInput input,CancellationToken ct){Check(input);return repository.CreateRoleAsync(tenant.EmpresaId,input,ct);}
    public Task<IReadOnlyList<PermissionDto>> PermissionsAsync(CancellationToken ct){Check();return repository.PermissionsAsync(ct);}
    public Task AssignRolesAsync(Guid membershipId,IReadOnlyList<Guid> ids,CancellationToken ct){Check();ValidateIds(ids);return repository.AssignRolesAsync(tenant.EmpresaId,membershipId,ids,ct);}
    public Task AssignPermissionsAsync(Guid roleId,IReadOnlyList<Guid> ids,CancellationToken ct){Check();ValidateIds(ids);return repository.AssignPermissionsAsync(tenant.EmpresaId,roleId,ids,ct);}
    private void Check(object? input=null){permissions.Require("Seguridad:Manage");if(input is not null&&!Validator.TryValidateObject(input,new ValidationContext(input),[],true))throw new ApplicationError(400,"VALIDATION","Datos de seguridad inválidos.");}
    private static void ValidateIds(IReadOnlyList<Guid> ids){if(ids.Count>500||ids.Contains(Guid.Empty)||ids.Distinct().Count()!=ids.Count)throw new ApplicationError(400,"VALIDATION","Asignaciones inválidas.");}
}
