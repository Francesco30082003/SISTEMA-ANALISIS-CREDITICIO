using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Security;
public sealed class SecurityAdministrationRepository(MapanDbContext db,AuditWriter audit,IPasswordService passwords):ISecurityAdministrationRepository
{
    public async Task<IReadOnlyList<Guid>> UserRolesAsync(Guid empresaId,Guid id,CancellationToken ct)
    {
        if(!await db.Set<UsuarioEmpresa>().AnyAsync(x=>x.EmpresaId==empresaId&&x.UsuarioEmpresaId==id,ct))throw ApplicationError.NotFound();
        return await (from a in db.Set<UsuarioEmpresaRol>().AsNoTracking() join r in db.Set<Rol>() on a.RolId equals r.RolId where a.UsuarioEmpresaId==id&&r.EmpresaId==empresaId select r.RolId).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<Guid>> RolePermissionsAsync(Guid empresaId,Guid id,CancellationToken ct)
    {
        if(!await db.Set<Rol>().AnyAsync(x=>x.EmpresaId==empresaId&&x.RolId==id,ct))throw ApplicationError.NotFound();
        return await db.Set<RolPermiso>().AsNoTracking().Where(x=>x.RolId==id).Select(x=>x.PermisoId).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<UserDto>> UsersAsync(Guid empresaId,CancellationToken ct)=>await (from membership in db.Set<UsuarioEmpresa>().AsNoTracking()
        join user in db.Set<Usuario>().AsNoTracking() on membership.UsuarioId equals user.UsuarioId where membership.EmpresaId==empresaId orderby user.NombreUsuario
        select new UserDto(membership.UsuarioEmpresaId,user.UsuarioId,user.NombreUsuario,user.Correo,user.Nombres,user.Apellidos,membership.Estado)).ToListAsync(ct);
    public async Task<Guid> CreateUserAsync(Guid empresaId,UserInput input,CancellationToken ct)
    {
        var user=new Usuario {UsuarioId=Guid.NewGuid(),NombreUsuario=input.NombreUsuario,Correo=input.Correo,Nombres=input.Nombres,Apellidos=input.Apellidos,PasswordHash="",Estado="ACTIVO"};
        user.PasswordHash=passwords.Hash(user,input.Password);var membership=new UsuarioEmpresa {UsuarioEmpresaId=Guid.NewGuid(),EmpresaId=empresaId,UsuarioId=user.UsuarioId,Estado="ACTIVO"};
        db.AddRange(user,membership);audit.Add("seguridad.usuario_empresa",membership.UsuarioEmpresaId,"CREAR_USUARIO");await db.SaveChangesAsync(ct);return membership.UsuarioEmpresaId;
    }
    public async Task<IReadOnlyList<RoleDto>> RolesAsync(Guid empresaId,CancellationToken ct)=>await db.Set<Rol>().AsNoTracking().Where(r=>r.EmpresaId==empresaId).OrderBy(r=>r.Codigo).Select(r=>new RoleDto(r.RolId,r.Codigo,r.Nombre,r.Descripcion,r.Estado)).ToListAsync(ct);
    public async Task<Guid> CreateRoleAsync(Guid empresaId,RoleInput input,CancellationToken ct)
    {
        var role=new Rol {RolId=Guid.NewGuid(),EmpresaId=empresaId,Codigo=input.Codigo,Nombre=input.Nombre,Descripcion=input.Descripcion,Estado="ACTIVO"};db.Add(role);audit.Add("seguridad.rol",role.RolId,"CREAR");await db.SaveChangesAsync(ct);return role.RolId;
    }
    public async Task<IReadOnlyList<PermissionDto>> PermissionsAsync(CancellationToken ct)=>await db.Set<Permiso>().AsNoTracking().OrderBy(p=>p.Codigo).Select(p=>new PermissionDto(p.PermisoId,p.Codigo,p.Nombre)).ToListAsync(ct);
    public async Task AssignRolesAsync(Guid empresaId,Guid membershipId,IReadOnlyList<Guid> roleIds,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var membership=(await db.Set<UsuarioEmpresa>().FromSqlInterpolated($"SELECT * FROM seguridad.usuario_empresa WHERE empresa_id={empresaId} AND usuario_empresa_id={membershipId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        if(await db.Set<Rol>().CountAsync(r=>r.EmpresaId==empresaId&&roleIds.Contains(r.RolId)&&r.Estado=="ACTIVO",ct)!=roleIds.Count)throw ApplicationError.Forbidden();
        var old=await db.Set<UsuarioEmpresaRol>().Where(r=>r.UsuarioEmpresaId==membership.UsuarioEmpresaId).ToListAsync(ct);
        db.RemoveRange(old.Where(r=>!roleIds.Contains(r.RolId)));
        foreach(var id in roleIds.Except(old.Select(r=>r.RolId)))db.Add(new UsuarioEmpresaRol{UsuarioEmpresaId=membershipId,RolId=id});
        audit.Add("seguridad.usuario_empresa",membershipId,"ASIGNAR_ROLES");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task AssignPermissionsAsync(Guid empresaId,Guid roleId,IReadOnlyList<Guid> permissionIds,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        if((await db.Set<Rol>().FromSqlInterpolated($"SELECT * FROM seguridad.rol WHERE empresa_id={empresaId} AND rol_id={roleId} FOR UPDATE").ToListAsync(ct)).Count!=1)throw ApplicationError.NotFound();
        if(await db.Set<Permiso>().CountAsync(p=>permissionIds.Contains(p.PermisoId),ct)!=permissionIds.Count)throw ApplicationError.NotFound();
        var old=await db.Set<RolPermiso>().Where(p=>p.RolId==roleId).ToListAsync(ct);db.RemoveRange(old.Where(p=>!permissionIds.Contains(p.PermisoId)));
        foreach(var id in permissionIds.Except(old.Select(p=>p.PermisoId)))db.Add(new RolPermiso{RolId=roleId,PermisoId=id});
        audit.Add("seguridad.rol",roleId,"ASIGNAR_PERMISOS");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
