using Mapan.Application.Security;

namespace Mapan.Application.Investigacion;

public sealed record ConsultaResumenDto(Guid ConsultaExternaId,string TipoConsulta,string Estado,DateTimeOffset FechaInicio,DateTimeOffset? FechaFin,string? MensajeError,string? Origen);
public sealed record BuroResumenDto(int? Score,string? BandaRiesgo,decimal? ProbabilidadMora,decimal DeudaTotal,
    decimal CuotaTotalMensual,int OperacionesVencidas,int MaximoDiasVencidoUltimos3Meses,decimal MontoDemandaJudicial,
    decimal MontoCarteraCastigada,int? MoraActualMaxDias,int? MoraHistoricaMaxDias);
public sealed record JudicialResumenDto(bool TieneProcesos,int NumeroProcesos,string? MateriaPrincipal,string? GravedadMaxima);
public sealed record AvalResumenDto(bool EsGaranteActivo,int OperacionesComoGarante,bool TieneMoraComoGarante);
public sealed record InvestigacionResultadoDto(Guid SolicitudCreditoId,IReadOnlyList<ConsultaResumenDto> Consultas,
    BuroResumenDto? Buro,JudicialResumenDto? Judicial,AvalResumenDto? Aval,DateTimeOffset? UltimaEjecucion);

public interface IInvestigacionRepository
{
    Task<InvestigacionResultadoDto?> GetUltimaAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<InvestigacionResultadoDto> EjecutarAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct);
}

public sealed class InvestigacionService(IInvestigacionRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<InvestigacionResultadoDto?> GetUltimaAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Investigacion:Read");return repository.GetUltimaAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<InvestigacionResultadoDto> EjecutarAsync(Guid solicitudId,CancellationToken ct)
    {permissions.Require("Investigacion:Execute");return repository.EjecutarAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudId,ct);}
}
