using Mapan.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/seguridad")]
public sealed class SeguridadController(SecurityAdministrationService service):ControllerBase
{
    [HttpGet("usuarios")]public async Task<ActionResult<IReadOnlyList<UserDto>>> Users(CancellationToken ct)=>Ok(await service.UsersAsync(ct));
    [HttpGet("usuarios/{id:guid}/roles")]public async Task<IActionResult> UserRoles(Guid id,CancellationToken ct)=>Ok(await service.UserRolesAsync(id,ct));
    [HttpGet("roles/{id:guid}/permisos")]public async Task<IActionResult> RolePermissions(Guid id,CancellationToken ct)=>Ok(await service.RolePermissionsAsync(id,ct));
    [HttpPost("usuarios")]public async Task<ActionResult<Guid>> CreateUser(UserInput input,CancellationToken ct)=>Ok(await service.CreateUserAsync(input,ct));
    [HttpGet("roles")]public async Task<ActionResult<IReadOnlyList<RoleDto>>> Roles(CancellationToken ct)=>Ok(await service.RolesAsync(ct));
    [HttpPost("roles")]public async Task<ActionResult<Guid>> CreateRole(RoleInput input,CancellationToken ct)=>Ok(await service.CreateRoleAsync(input,ct));
    [HttpGet("permisos")]public async Task<ActionResult<IReadOnlyList<PermissionDto>>> Permissions(CancellationToken ct)=>Ok(await service.PermissionsAsync(ct));
    [HttpPut("usuarios/{membershipId:guid}/roles")]public async Task<IActionResult> AssignRoles(Guid membershipId,[FromBody]Guid[] roleIds,CancellationToken ct){await service.AssignRolesAsync(membershipId,roleIds,ct);return NoContent();}
    [HttpPut("roles/{roleId:guid}/permisos")]public async Task<IActionResult> AssignPermissions(Guid roleId,[FromBody]Guid[] permissionIds,CancellationToken ct){await service.AssignPermissionsAsync(roleId,permissionIds,ct);return NoContent();}
}
