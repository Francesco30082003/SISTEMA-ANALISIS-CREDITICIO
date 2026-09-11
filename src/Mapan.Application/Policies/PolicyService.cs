using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;

namespace Mapan.Application.Policies;
public sealed record PolicyInput([Required,MaxLength(60)]string Codigo,[Required,MaxLength(180)]string Nombre,string? Descripcion,Guid? ProductoCreditoId,string Estado="ACTIVA");
public sealed record VersionInput(DateTimeOffset VigenteDesde,DateTimeOffset? VigenteHasta);
public sealed record ParameterInput([Required,MaxLength(100)]string Codigo,[Required,MaxLength(180)]string Nombre,string TipoDato,string? ValorTexto,decimal? ValorNumerico,bool? ValorBooleano,DateOnly? ValorFecha,string? ValorJson);
// Resultado se interpreta según Etapa: para ANALISIS es CAPACIDAD_COMPATIBLE/CAPACIDAD_NO_COMPATIBLE/REVISION_ADICIONAL
// (como hoy); para PREEVALUACION es la acción CONTINUAR/ALERTA/REQUIERE_EXCEPCION/BLOQUEAR (ver PreevaluacionDecisions).
public sealed record RuleInput([Required,MaxLength(100)]string Codigo,[Required,MaxLength(180)]string Nombre,string? Descripcion,int Prioridad,string Severidad,string CondicionJson,string Resultado,bool Activa=true,string Etapa="ANALISIS");
public sealed record PolicyDetail(PoliticaCredito Politica,IReadOnlyList<PoliticaVersion> Versiones);
public sealed record PolicyVersionDetail(PoliticaVersion Version,IReadOnlyList<Parametro> Parametros,IReadOnlyList<Regla> Reglas,bool Editable);
public interface IPolicyRepository
{
    Task<IReadOnlyList<PoliticaCredito>> ListAsync(Guid tenant,CancellationToken ct);
    Task<PolicyDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct);
    Task<Guid> SaveAsync(Guid tenant,Guid? id,PolicyInput input,CancellationToken ct);
    Task<Guid> CreateVersionAsync(Guid tenant,Guid member,Guid id,VersionInput input,CancellationToken ct);
    Task<PolicyVersionDetail> VersionAsync(Guid tenant,Guid id,CancellationToken ct);
    Task StateAsync(Guid tenant,Guid member,Guid id,string state,CancellationToken ct);
    Task SaveParameterAsync(Guid tenant,Guid version,Guid? id,ParameterInput input,CancellationToken ct);
    Task SaveRuleAsync(Guid tenant,Guid version,Guid? id,RuleInput input,CancellationToken ct);
    Task DeleteItemAsync(Guid tenant,Guid version,Guid id,bool rule,CancellationToken ct);
}
public sealed class PolicyService(IPolicyRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    private void Check(object? value=null){permissions.Require(value is null?"Politicas:Read":"Politicas:Manage");if(value is not null&&!Validator.TryValidateObject(value,new(value),[],true))throw new ApplicationError(400,"VALIDATION","Revisa los datos de la política.");}
    public Task<IReadOnlyList<PoliticaCredito>> ListAsync(CancellationToken ct){Check();return repository.ListAsync(tenant.EmpresaId,ct);}
    public Task<PolicyDetail> GetAsync(Guid id,CancellationToken ct){Check();return repository.GetAsync(tenant.EmpresaId,id,ct);}
    public Task<Guid> SaveAsync(Guid? id,PolicyInput input,CancellationToken ct){Check(input);return repository.SaveAsync(tenant.EmpresaId,id,input,ct);}
    public Task<Guid> CreateVersionAsync(Guid id,VersionInput input,CancellationToken ct){Check(input);return repository.CreateVersionAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,input,ct);}
    public Task<PolicyVersionDetail> VersionAsync(Guid id,CancellationToken ct){Check();return repository.VersionAsync(tenant.EmpresaId,id,ct);}
    public Task StateAsync(Guid id,string state,CancellationToken ct){Check(state);return repository.StateAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,state,ct);}
    public Task ParameterAsync(Guid version,Guid? id,ParameterInput input,CancellationToken ct){Check(input);return repository.SaveParameterAsync(tenant.EmpresaId,version,id,input,ct);}
    public Task RuleAsync(Guid version,Guid? id,RuleInput input,CancellationToken ct){Check(input);return repository.SaveRuleAsync(tenant.EmpresaId,version,id,input,ct);}
    public Task DeleteAsync(Guid version,Guid id,bool rule,CancellationToken ct){Check(id);return repository.DeleteItemAsync(tenant.EmpresaId,version,id,rule,ct);}
}
