using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;

namespace Mapan.Application.Modelos;
public sealed record ModeloInput([Required,MaxLength(100)]string Codigo,[Required,MaxLength(200)]string Nombre,string? Descripcion,[Required,MaxLength(200)]string Objetivo);
public sealed record ModeloVersionInput([Required,MaxLength(100)]string Algoritmo,string? Descripcion,string? ArtefactoUri,[Required]string EsquemaCaracteristicas,string? Hiperparametros,
    DateOnly? FechaDatosDesde,DateOnly? FechaDatosHasta,int? CantidadRegistros,int? CantidadPositivos,int? CantidadNegativos,
    decimal? RocAuc,decimal? PrecisionScore,decimal? RecallScore,decimal? F1Score,decimal? AccuracyScore,decimal? UmbralDecision);
public sealed record ModeloDetail(Modelo Modelo,IReadOnlyList<ModeloVersion> Versiones);
public interface IModeloRepository
{
    Task<IReadOnlyList<Modelo>> ListAsync(Guid tenant,CancellationToken ct);
    Task<ModeloDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct);
    Task<Guid> SaveAsync(Guid tenant,Guid? id,ModeloInput input,CancellationToken ct);
    Task StateAsync(Guid tenant,Guid id,string state,CancellationToken ct);
    Task<Guid> CreateVersionAsync(Guid tenant,Guid id,ModeloVersionInput input,CancellationToken ct);
    Task SaveVersionAsync(Guid tenant,Guid id,ModeloVersionInput input,CancellationToken ct);
    Task VersionStateAsync(Guid tenant,Guid id,string state,CancellationToken ct);
}
public sealed class ModeloService(IModeloRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    private void Check(object? value=null){permissions.Require(value is null?"Modelos:Read":"Modelos:Manage");if(value is not null&&!Validator.TryValidateObject(value,new(value),[],true))throw new ApplicationError(400,"VALIDATION","Revisa los datos del modelo.");}
    public Task<IReadOnlyList<Modelo>> ListAsync(CancellationToken ct){Check();return repository.ListAsync(tenant.EmpresaId,ct);}
    public Task<ModeloDetail> GetAsync(Guid id,CancellationToken ct){Check();return repository.GetAsync(tenant.EmpresaId,id,ct);}
    public Task<Guid> SaveAsync(Guid? id,ModeloInput input,CancellationToken ct){Check(input);return repository.SaveAsync(tenant.EmpresaId,id,input,ct);}
    public Task StateAsync(Guid id,string state,CancellationToken ct){Check(state);return repository.StateAsync(tenant.EmpresaId,id,state,ct);}
    public Task<Guid> CreateVersionAsync(Guid id,ModeloVersionInput input,CancellationToken ct){Check(input);return repository.CreateVersionAsync(tenant.EmpresaId,id,input,ct);}
    public Task SaveVersionAsync(Guid id,ModeloVersionInput input,CancellationToken ct){Check(input);return repository.SaveVersionAsync(tenant.EmpresaId,id,input,ct);}
    public Task VersionStateAsync(Guid id,string state,CancellationToken ct){Check(state);return repository.VersionStateAsync(tenant.EmpresaId,id,state,ct);}
}
