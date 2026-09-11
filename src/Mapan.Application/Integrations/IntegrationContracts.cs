using System.Text.Json;

namespace Mapan.Application.Integrations;

public sealed record RiskModelRequest(Guid AnalisisId,Guid ModeloVersionId,IReadOnlyDictionary<string,decimal?> Vector);
// Proveedor identifica de dónde vino la predicción — "ML_SERVICE" es el modelo propio real;
// "CLAUDE_TEMPORAL" marca una predicción de respaldo con IA mientras ese modelo no está entrenado
// (ver HybridRiskModelClient). Nunca debe confundirse una con otra en la UI de análisis.
// ResumenRiesgo/Sugerencias son narrativa opcional para que el analista entienda, en una frase, qué
// riesgo se corre al prestarle a este cliente y qué acción concreta podría cambiar esa evaluación
// (ej. pedir garante, reducir el monto). Solo el respaldo con IA (Claude) las llena hoy — un ml-service
// real puede omitirlas sin romper el contrato, por eso van al final con valor por defecto null.
public sealed record RiskModelResponse(Guid AnalisisId,Guid ModeloVersionId,decimal ProbabilidadIncumplimiento,
    decimal UmbralUtilizado,string ClasePredicha,string? NivelRiesgo,int TiempoInferenciaMs,IReadOnlyList<PredictionFactor> Factores,
    string Proveedor="ML_SERVICE",string? ResumenRiesgo=null,IReadOnlyList<string>? Sugerencias=null);
public sealed record PredictionFactor(string CodigoCaracteristica,decimal Contribucion,string Direccion);
public interface IRiskModelClient {Task<RiskModelResponse> PredictAsync(RiskModelRequest request,CancellationToken ct);}
public sealed record IntegrationResult(string Status,JsonElement? Data);
public interface IDocumentExtractionService {Task<IntegrationResult> ExtractAsync(Guid empresaId,Guid documentoVersionId,CancellationToken ct);}

// Investigación del cliente: cada proveedor devuelve información ya normalizada (ver Mapan.Domain,
// ningún módulo de dominio conoce el formato propio de Equifax/buró/judicial/aval). Un proveedor real
// debe lanzar en caso de falla/timeout — el llamador (InvestigacionRepository) decide qué hacer con eso
// (nunca fabricar datos, nunca tumbar la investigación completa por una fuente caída).
public sealed record DeudaPorFuente(string Fuente,decimal PorVencer,decimal Vencido,decimal DemandaJudicial,decimal CarteraCastigada,decimal SaldoDeuda,decimal? CuotaMensual);
public sealed record BuroCreditoResultado(int? Score,string? BandaRiesgo,decimal? ProbabilidadMora,decimal DeudaTotal,
    decimal CuotaTotalMensual,int OperacionesVencidas,int MaximoDiasVencidoUltimos3Meses,decimal MontoDemandaJudicial,
    decimal MontoCarteraCastigada,int? MoraActualMaxDias,int? MoraHistoricaMaxDias,IReadOnlyList<DeudaPorFuente> DesglosePorFuente);
public interface ICreditBureauProvider {Task<BuroCreditoResultado> ConsultarAsync(string tipoIdentificacion,string numeroIdentificacion,CancellationToken ct);}

public sealed record ProcesoJudicial(string Materia,string Estado,string Gravedad);
public sealed record JudicialResultado(bool TieneProcesos,int NumeroProcesos,IReadOnlyList<ProcesoJudicial> Procesos);
public interface IJudicialProvider {Task<JudicialResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct);}

public sealed record AvalResultado(bool EsGaranteActivo,int OperacionesComoGarante,bool TieneMoraComoGarante);
public interface IAvalProvider {Task<AvalResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct);}

// Verificación documental (Fase 6): analiza un documento ya cargado en busca de señales de fraude o
// inconsistencia (metadatos, legibilidad, coherencia con el tipo declarado) — distinto del flujo de
// DatoValidado (que confirma/corrige el CONTENIDO extraído), esto evalúa la AUTENTICIDAD del archivo.
public sealed record VerificacionDocumentoResultado(string Resultado,decimal ConfianzaPct,IReadOnlyList<string> Motivos);
public interface IDocumentVerificationProvider {Task<VerificacionDocumentoResultado> VerificarAsync(string tipoDocumento,string nombreArchivo,long tamanoBytes,CancellationToken ct);}

// Verificación laboral (IESS, Fase 6): no existe acceso a una API oficial del IESS en este entorno
// (confirmado en Fase 1) — mock únicamente, misma disciplina que MockJudicialProvider (sin scraping).
public sealed record VerificacionLaboralResultado(bool RelacionLaboralActiva,string? EmpleadorRegistrado,DateOnly? FechaAfiliacion,decimal? AporteMensual,string Estado);
public interface IVerificacionLaboralProvider {Task<VerificacionLaboralResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct);}
