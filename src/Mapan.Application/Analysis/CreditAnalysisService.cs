using System.Text.Json;
using Mapan.Application.Security;
using Mapan.Domain.Entities;

namespace Mapan.Application.Analysis;
public sealed record AnalysisSummary(Guid AnalisisId,string Solicitud,string Cliente,string Estado,int NumeroEjecucion,DateTimeOffset Fecha,string? Recomendacion);
public sealed record DecisionInput(string Decision,string? Comentario);
public sealed record ObligacionResumen(string? Institucion,string? TipoObligacion,decimal? SaldoActual,decimal? CuotaMensual,int DiasMoraActual,string? Estado);
public sealed record DocumentoResumen(string TipoDocumento,string NombreArchivo,int NumeroVersion,string EstadoAsociacion,bool Obligatorio,string? UsoDocumento);
public sealed record AnalysisDetail(Analisis Analisis,SnapshotFinanciero? Snapshot,VectorCaracteristicas? Vector,Recomendacion? Recomendacion,IReadOnlyList<Alerta> Alertas,JsonElement? Contexto,Prediccion? Prediccion,
    IReadOnlyList<ObligacionResumen> Obligaciones,IReadOnlyList<DocumentoResumen> Documentos);
public interface ICreditAnalysisRepository
{
    Task<Guid> ExecuteAsync(Guid tenant,Guid member,Guid request,CancellationToken ct);
    Task<IReadOnlyList<AnalysisSummary>> ListAsync(Guid tenant,Guid? request,CancellationToken ct);
    Task<AnalysisDetail> GetAsync(Guid tenant,Guid id,CancellationToken ct);
    Task ResolveAlertAsync(Guid tenant,Guid member,Guid id,CancellationToken ct);
    Task<IReadOnlyList<Alerta>> AlertsAsync(Guid tenant,CancellationToken ct);
    Task<Guid> DecideAsync(Guid tenant,Guid member,Guid analisisId,string decision,string? comment,CancellationToken ct);
}
public sealed class CreditAnalysisService(ICreditAnalysisRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<Guid> ExecuteAsync(Guid request,CancellationToken ct){permissions.Require("Analisis:Execute");return repository.ExecuteAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,request,ct);}
    public Task<IReadOnlyList<AnalysisSummary>> ListAsync(Guid? request,CancellationToken ct){permissions.Require("Analisis:Read");return repository.ListAsync(tenant.EmpresaId,request,ct);}
    public Task<AnalysisDetail> GetAsync(Guid id,CancellationToken ct){permissions.Require("Analisis:Read");return repository.GetAsync(tenant.EmpresaId,id,ct);}
    public Task<IReadOnlyList<Alerta>> AlertsAsync(CancellationToken ct){permissions.Require("Alertas:Read");return repository.AlertsAsync(tenant.EmpresaId,ct);}
    public Task ResolveAsync(Guid id,CancellationToken ct){permissions.Require("Analisis:Execute");return repository.ResolveAlertAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,ct);}
    // Camino de cierre para cuando NINGUNA ruta de aprobación aplica (o la empresa no configuró ninguna) —
    // sin esto, una solicitud sin flujo dinámico quedaba atascada en REVISION para siempre, contradiciendo
    // el propio requisito de que una cooperativa pueda "omitir el flujo y quedarse solo con la recomendación".
    // Si SÍ existe un flujo activo para esta recomendación, el repositorio rechaza esta vía — debe decidirse
    // desde la bandeja de aprobaciones (Workflow), nunca las dos a la vez.
    public Task<Guid> DecideAsync(Guid analisisId,string decision,string? comment,CancellationToken ct){permissions.Require("Solicitudes:Decidir");return repository.DecideAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,analisisId,decision,comment,ct);}
}
