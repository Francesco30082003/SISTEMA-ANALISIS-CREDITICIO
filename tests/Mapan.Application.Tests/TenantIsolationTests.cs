using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Mapan.Infrastructure.Persistence.Repositories;
using Mapan.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapan.Application.Tests;
public sealed class TenantIsolationTests
{
    [Fact]
    public async Task SecurityAssignmentQueriesCannotCrossTenant()
    {
        await using var db=Database();var current=new Context();var memberId=Guid.NewGuid();var roleId=Guid.NewGuid();
        db.Add(new UsuarioEmpresa{UsuarioEmpresaId=memberId,UsuarioId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),Estado="ACTIVO"});
        db.Add(new Rol{RolId=roleId,EmpresaId=Guid.NewGuid(),Codigo="OTHER",Nombre="Other company",Estado="ACTIVO"});await db.SaveChangesAsync();
        var repository=new SecurityAdministrationRepository(db,new AuditWriter(db,current,current,new HttpContextAccessor(),TimeProvider.System),new PasswordService());
        Assert.Equal(404,(await Assert.ThrowsAsync<ApplicationError>(()=>repository.UserRolesAsync(current.EmpresaId,memberId,default))).Status);
        Assert.Equal(404,(await Assert.ThrowsAsync<ApplicationError>(()=>repository.RolePermissionsAsync(current.EmpresaId,roleId,default))).Status);
    }
    [Fact]
    public async Task DocumentReviewCannotReadAnotherCompany()
    {
        await using var db=Database();var current=new Context();var link=new SolicitudDocumento{SolicitudDocumentoId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),SolicitudCreditoId=Guid.NewGuid(),DocumentoVersionId=Guid.NewGuid(),Estado="PENDIENTE",AsociadoPorUsuarioEmpresaId=Guid.NewGuid()};db.Add(link);await db.SaveChangesAsync();
        var repository=new DocumentoRepository(db,new AuditWriter(db,current,current,new HttpContextAccessor(),TimeProvider.System),null!,null!,NullLogger<DocumentoRepository>.Instance);
        var error=await Assert.ThrowsAsync<ApplicationError>(()=>repository.ReviewAsync(current.EmpresaId,link.SolicitudDocumentoId,default));
        Assert.Equal(404,error.Status);
    }
    [Fact]
    public async Task DocumentReviewReturnsOnlyItsVersionAndPreservesRevisions()
    {
        await using var db=Database();var current=new Context();var id=Guid.NewGuid();var version=Guid.NewGuid();
        db.Add(new SolicitudDocumento{SolicitudDocumentoId=id,EmpresaId=current.EmpresaId,SolicitudCreditoId=Guid.NewGuid(),DocumentoVersionId=version,Estado="PENDIENTE",AsociadoPorUsuarioEmpresaId=current.UsuarioEmpresaId});
        foreach(var revision in new[]{1,2})db.Add(new DatoValidado{DatoValidadoId=Guid.NewGuid(),EmpresaId=current.EmpresaId,SolicitudDocumentoId=id,CodigoCampo="ingreso",NumeroRevision=revision,TipoDato="NUMERO",ValorNumerico=revision*100,Estado="CORREGIDO",ValidadoPorUsuarioEmpresaId=current.UsuarioEmpresaId});
        db.Add(new DatoExtraido{DatoExtraidoId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),DocumentoVersionId=version,CodigoCampo="privado",TipoDato="TEXTO"});await db.SaveChangesAsync();
        var repository=new DocumentoRepository(db,new AuditWriter(db,current,current,new HttpContextAccessor(),TimeProvider.System),null!,null!,NullLogger<DocumentoRepository>.Instance);
        var result=await repository.ReviewAsync(current.EmpresaId,id,default);
        Assert.Empty(result.Extraidos);Assert.Equal(new[]{2,1},result.Validaciones.Select(x=>x.NumeroRevision));
    }
    [Fact]
    public async Task CompaniesAreLimitedToActiveMemberships()
    {
        await using var db=Database();var user=new Context();
        var own=Company("OWN");var other=Company("OTHER");db.AddRange(own,other);
        db.Add(new UsuarioEmpresa {UsuarioEmpresaId=user.UsuarioEmpresaId,UsuarioId=user.UsuarioId,EmpresaId=own.EmpresaId,Estado="ACTIVO"});
        await db.SaveChangesAsync();
        var result=await new EmpresaRepository(db,user).ObtenerEmpresasAsync(default);
        Assert.Single(result);Assert.Equal(own.EmpresaId,result[0].EmpresaId);
    }
    [Fact]
    public async Task ClientDetailAndUpdateCannotCrossTenant()
    {
        await using var db=Database();var current=new Context();var other=Guid.NewGuid();
        var client=new Cliente {ClienteId=Guid.NewGuid(),EmpresaId=other,TipoPersona="NATURAL",TipoIdentificacion="TEST",NumeroIdentificacion="TEST-ONLY",Estado="ACTIVO"};db.Add(client);await db.SaveChangesAsync();
        var repository=new ClienteRepository(db,new AuditWriter(db,current,current,new HttpContextAccessor(),TimeProvider.System),TimeProvider.System);
        Assert.Null(await repository.GetAsync(current.EmpresaId,client.ClienteId,default));
        Assert.Equal(0,(await repository.ListAsync(current.EmpresaId,null,1,20,default)).Total);
        var error=await Assert.ThrowsAsync<ApplicationError>(()=>repository.InactivateAsync(current.EmpresaId,client.ClienteId,default));
        Assert.Equal(404,error.Status);Assert.Equal("ACTIVO",client.Estado);
    }
    [Fact]
    public async Task RoleFromDifferentCompanyNeverGrantsPermissions()
    {
        await using var db=Database();var current=new Context();var company=Company("OWN");company.EmpresaId=current.EmpresaId;
        db.Add(company);db.Add(new Usuario {UsuarioId=current.UsuarioId,NombreUsuario="test",Correo="test@example.invalid",PasswordHash="unused",Nombres="Test",Apellidos="Test",Estado="ACTIVO"});
        db.Add(new UsuarioEmpresa {UsuarioEmpresaId=current.UsuarioEmpresaId,UsuarioId=current.UsuarioId,EmpresaId=current.EmpresaId,Estado="ACTIVO"});
        var role=new Rol {RolId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),Codigo="OTHER",Nombre="Other",Estado="ACTIVO"};
        var permission=new Permiso {PermisoId=Guid.NewGuid(),Codigo="TEST_PERMISSION",Nombre="Test"};db.AddRange(role,permission);
        db.Add(new UsuarioEmpresaRol {UsuarioEmpresaId=current.UsuarioEmpresaId,RolId=role.RolId});
        db.Add(new RolPermiso {RolId=role.RolId,PermisoId=permission.PermisoId});await db.SaveChangesAsync();
        var identity=await new AuthenticationRepository(db,new HttpContextAccessor(),TimeProvider.System).GetIdentityAsync(current.UsuarioId,current.EmpresaId,default);
        Assert.NotNull(identity);Assert.Empty(identity.Roles);Assert.Empty(identity.Permisos);
    }
    private static MapanDbContext Database()=>new(new DbContextOptionsBuilder<MapanDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Empresa Company(string code)=>new(){EmpresaId=Guid.NewGuid(),Codigo=code,NombreLegal=code,PaisCodigo="EC",MonedaCodigo="USD",Estado="ACTIVA"};
    private sealed class Context:ICurrentUser,ICurrentTenant {public Guid UsuarioId{get;}=Guid.NewGuid();public string NombreUsuario=>"test";public Guid EmpresaId{get;}=Guid.NewGuid();public Guid UsuarioEmpresaId{get;}=Guid.NewGuid();}
}
