using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Operations;
public sealed record OperationQuery(Guid? Id=null,DateTimeOffset? Desde=null,DateTimeOffset? Hasta=null,Guid? Usuario=null,string? Entidad=null,string? Accion=null,string? CorrelationId=null,int Page=1,int Size=50);
public interface IOperationsRepository
{
    Task<object> ReadAsync(string area,Guid tenant,Guid member,OperationQuery query,CancellationToken ct);
    Task<Guid> WriteAsync(string area,Guid tenant,Guid member,Guid? id,JsonElement input,CancellationToken ct);
}
public sealed class OperationsService(IOperationsRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    // "modelos" is handled by the dedicated Mapan.Application.Modelos.ModeloService/ModelosController, not by this generic multiplexer.
    private static string Permission(string area)=>area switch{"auditoria"=>"Auditoria","prestamos" or "desempeno"=>"Prestamos","workflow" or "rutas" or "decisiones"=>"Workflow","integraciones"=>"Integraciones",_=>throw ApplicationError.NotFound()};
    public Task<object> ReadAsync(string area,OperationQuery query,CancellationToken ct){permissions.Require(Permission(area)+":Read");Paging.Validate(query.Page,query.Size);return repository.ReadAsync(area,tenant.EmpresaId,tenant.UsuarioEmpresaId,query,ct);}
    public Task<Guid> WriteAsync(string area,Guid? id,JsonElement input,CancellationToken ct){if(area=="auditoria")throw ApplicationError.Forbidden();permissions.Require(Permission(area)+":Manage");return repository.WriteAsync(area,tenant.EmpresaId,tenant.UsuarioEmpresaId,id,input,ct);}
}
