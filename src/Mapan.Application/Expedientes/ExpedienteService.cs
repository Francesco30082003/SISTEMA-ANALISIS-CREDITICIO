using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;

namespace Mapan.Application.Expedientes;
public sealed record ActividadInput([Required,MaxLength(30)] string TipoActividad,[MaxLength(200)] string? EmpleadorNegocio,
    [MaxLength(150)] string? CargoActividad,DateOnly? FechaInicio,[MaxLength(20)] string? Ruc,[MaxLength(300)] string? Descripcion,bool EsPrincipal);
public sealed record FuenteInput(Guid? ActividadEconomicaId,[Required,MaxLength(50)] string TipoIngreso,[MaxLength(200)] string? Descripcion,
    [Required,StringLength(3,MinimumLength=3)] string MonedaCodigo,bool EsRecurrente);
public sealed record PeriodoInput(DateOnly PeriodoInicio,DateOnly PeriodoFin,decimal? MontoBruto,decimal MontoNeto,[MaxLength(300)] string? Observacion);
public sealed record GastoInput([Required,MaxLength(50)] string TipoGasto,[MaxLength(200)] string? Descripcion,decimal MontoMensual);
public sealed record ObligacionInput([MaxLength(200)] string? Institucion,[MaxLength(50)] string? TipoObligacion,
    [MaxLength(80)] string? NumeroOperacionMascara,decimal? MontoOriginal,decimal? SaldoActual,decimal? CuotaMensual,
    int DiasMoraActual,int MaxDiasMoraHistorico,[MaxLength(30)] string? Estado,bool EsGarante);
public sealed record ExpedienteDto(IReadOnlyList<ActividadEconomica> Actividades,IReadOnlyList<FuenteIngreso> Fuentes,
    IReadOnlyList<IngresoPeriodo> Periodos,IReadOnlyList<Gasto> Gastos,IReadOnlyList<Obligacion> Obligaciones);
public interface IExpedienteRepository
{
    Task<ExpedienteDto> GetAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<Guid> SaveActividadAsync(Guid empresaId,Guid solicitudId,Guid? id,ActividadInput input,CancellationToken ct);
    Task<Guid> SaveFuenteAsync(Guid empresaId,Guid solicitudId,Guid? id,FuenteInput input,CancellationToken ct);
    Task<Guid> SavePeriodoAsync(Guid empresaId,Guid solicitudId,Guid fuenteId,Guid? id,PeriodoInput input,CancellationToken ct);
    Task<Guid> SaveGastoAsync(Guid empresaId,Guid solicitudId,Guid? id,GastoInput input,CancellationToken ct);
    Task<Guid> SaveObligacionAsync(Guid empresaId,Guid solicitudId,Guid? id,ObligacionInput input,CancellationToken ct);
    Task DeletePeriodoAsync(Guid empresaId,Guid solicitudId,Guid fuenteId,Guid id,CancellationToken ct);
    Task DeleteGastoAsync(Guid empresaId,Guid solicitudId,Guid id,CancellationToken ct);
    Task DeleteObligacionAsync(Guid empresaId,Guid solicitudId,Guid id,CancellationToken ct);
}
public sealed class ExpedienteService(IExpedienteRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<ExpedienteDto> GetAsync(Guid solicitudId,CancellationToken ct) {permissions.Require("Solicitudes:Read");return repository.GetAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<Guid> SaveActividadAsync(Guid solicitudId,Guid? id,ActividadInput input,CancellationToken ct) {Validate(input);return repository.SaveActividadAsync(tenant.EmpresaId,solicitudId,id,input,ct);}
    public Task<Guid> SaveFuenteAsync(Guid solicitudId,Guid? id,FuenteInput input,CancellationToken ct) {Validate(input);return repository.SaveFuenteAsync(tenant.EmpresaId,solicitudId,id,input,ct);}
    public Task<Guid> SavePeriodoAsync(Guid solicitudId,Guid fuenteId,Guid? id,PeriodoInput input,CancellationToken ct) {
        Validate(input);if(input.PeriodoFin<input.PeriodoInicio||input.MontoNeto<0||input.MontoBruto<0)throw Invalid();
        return repository.SavePeriodoAsync(tenant.EmpresaId,solicitudId,fuenteId,id,input,ct);
    }
    public Task<Guid> SaveGastoAsync(Guid solicitudId,Guid? id,GastoInput input,CancellationToken ct) {Validate(input);if(input.MontoMensual<0)throw Invalid();return repository.SaveGastoAsync(tenant.EmpresaId,solicitudId,id,input,ct);}
    public Task<Guid> SaveObligacionAsync(Guid solicitudId,Guid? id,ObligacionInput input,CancellationToken ct) {
        Validate(input);if(input.SaldoActual<0||input.CuotaMensual<0||input.MontoOriginal<0||input.DiasMoraActual<0||input.MaxDiasMoraHistorico<0)throw Invalid();
        return repository.SaveObligacionAsync(tenant.EmpresaId,solicitudId,id,input,ct);
    }
    public Task DeletePeriodoAsync(Guid solicitudId,Guid fuenteId,Guid id,CancellationToken ct) {permissions.Require("Solicitudes:Update");return repository.DeletePeriodoAsync(tenant.EmpresaId,solicitudId,fuenteId,id,ct);}
    public Task DeleteGastoAsync(Guid solicitudId,Guid id,CancellationToken ct) {permissions.Require("Solicitudes:Update");return repository.DeleteGastoAsync(tenant.EmpresaId,solicitudId,id,ct);}
    public Task DeleteObligacionAsync(Guid solicitudId,Guid id,CancellationToken ct) {permissions.Require("Solicitudes:Update");return repository.DeleteObligacionAsync(tenant.EmpresaId,solicitudId,id,ct);}
    private void Validate(object input) {permissions.Require("Solicitudes:Update");if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true))throw Invalid();}
    private static ApplicationError Invalid()=>new(400,"VALIDATION","Datos del expediente inválidos.");
}
