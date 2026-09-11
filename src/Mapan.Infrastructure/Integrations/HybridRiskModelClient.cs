using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using Mapan.Application.Common;
using Mapan.Application.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Integrations;

// Respaldo con IA (Claude) para cuando services/ml-service todavía no tiene un artefacto entrenado
// (503 MODEL_NOT_CONFIGURED) o no responde — sirve de "modelo productivo" temporal para poder probar
// el flujo completo de extremo a extremo (análisis → predicción → recomendación → aprobación) mientras
// se entrena el modelo propio con los datos reales que MAPAN vaya acumulando. Nunca sustituye a
// ml-service cuando este sí contesta; solo entra cuando ese servicio falla o está desconfigurado.
// La respuesta queda marcada con Proveedor="CLAUDE_TEMPORAL" para que nunca se confunda con una
// predicción del modelo real (ver uso en CreditAnalysisRepository, campo mlStatus).
public sealed class HybridRiskModelClient(RiskModelClient real, IConfiguration configuration, ILogger<HybridRiskModelClient> logger) : IRiskModelClient
{
    private const decimal UmbralPredeterminado = 0.5m;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RiskModelResponse> PredictAsync(RiskModelRequest request, CancellationToken ct)
    {
        try { return await real.PredictAsync(request, ct); }
        catch (Exception e) when (e is ApplicationError or HttpRequestException or TaskCanceledException)
        {
            var apiKey = configuration["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) throw;
            logger.LogInformation(e, "ml-service no disponible; se usa Claude como modelo temporal para el análisis {AnalisisId}", request.AnalisisId);
            return await PredictWithClaudeAsync(apiKey, request, ct);
        }
    }

    private async Task<RiskModelResponse> PredictWithClaudeAsync(string apiKey, RiskModelRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        AnthropicClient client = new() { ApiKey = apiKey };
        var featureCodes = request.Vector.Keys.ToArray();
        var vectorJson = JsonSerializer.Serialize(request.Vector, JsonOptions);

        var schema = new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                probabilidad_incumplimiento = new { type = "number", description = "0 a 1: probabilidad estimada de que el cliente incumpla este crédito." },
                nivel_riesgo = new { type = "string", @enum = new[] { "BAJO", "MEDIO", "ALTO" } },
                resumen_riesgo = new { type = "string", description = "1-2 frases en español dirigidas al analista: qué riesgo concreto se corre al prestarle a este cliente (no repitas el nivel_riesgo, explica el porqué en términos de los factores del vector)." },
                sugerencias = new { type = "array", items = new { type = "string" }, description = "0 a 3 acciones concretas y específicas a este caso que ayudarían a la cooperativa a confiar más (ej. exigir garante, reducir monto o plazo, pedir un período de prueba de pagos) o a desconfiar más (ej. verificar in situ, pedir documentación adicional). Vacío si no hay ninguna sugerencia relevante — no rellenes con genéricos." },
                factores = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            codigo_caracteristica = new { type = "string", @enum = featureCodes },
                            contribucion = new { type = "number", description = "Magnitud relativa de la contribución de esta característica a la predicción." },
                            direccion = new { type = "string", @enum = new[] { "AUMENTA_RIESGO", "REDUCE_RIESGO", "NEUTRO" } },
                        },
                        required = new[] { "codigo_caracteristica", "contribucion", "direccion" },
                        additionalProperties = false,
                    },
                },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "probabilidad_incumplimiento", "nivel_riesgo", "resumen_riesgo", "sugerencias", "factores" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-sonnet-5",
            // El vector puede traer hasta ~20 características (una entrada de "factores" cada una), más el
            // resumen narrativo y hasta 3 sugerencias — con vectores grandes (muchas señales de buró/judicial
            // presentes) 2048 tokens no alcanzaban y Claude cortaba el JSON a la mitad de un string, lo que
            // rompía el parseo y dejaba el análisis sin predicción ("Modelo temporalmente no disponible") aun
            // cuando Claude sí había respondido — visto en producción con JsonException "Expected end of
            // string, but instead reached end of data" en factores[8].
            MaxTokens = 4096,
            System = "Eres un modelo temporal de riesgo crediticio para una cooperativa de crédito ecuatoriana, mientras esta " +
                     "entrena su propio modelo con datos históricos reales. Dado un vector de características financieras de " +
                     "una solicitud de crédito, estima la probabilidad de incumplimiento (0 a 1) con criterio conservador de " +
                     "microfinanzas: una alta relación cuota/ingreso o deuda/ingreso, mora histórica elevada, o baja antigüedad " +
                     "de actividad económica aumentan el riesgo; ingreso disponible holgado y buena antigüedad lo reducen. " +
                     "Reporta solo características que aparezcan en el vector recibido. Además de la probabilidad, escribe un " +
                     "resumen breve y concreto (no genérico) del riesgo real de prestarle a este cliente específico, y hasta " +
                     "3 sugerencias accionables — específicas a lo que ves en el vector — que ayudarían a la cooperativa a " +
                     "confiar más o a desconfiar más de esta operación. Esta predicción es un respaldo temporal y siempre " +
                     "será revisada por una persona antes de decidir.",
            OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
            Messages = [new() { Role = Role.User, Content = "Vector de características (JSON): " + vectorJson }],
        }, ct);

        var text = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text)) throw ApplicationError.Configuration("Claude no devolvió una predicción.");
        var parsed = JsonSerializer.Deserialize<ClaudeRiskResponse>(text, JsonOptions)
            ?? throw ApplicationError.Configuration("Predicción de Claude vacía.");
        if (parsed.ProbabilidadIncumplimiento is < 0 or > 1) throw ApplicationError.Configuration("Probabilidad fuera de rango.");

        var probabilidad = (decimal)parsed.ProbabilidadIncumplimiento;
        var clase = probabilidad >= UmbralPredeterminado ? "INCUMPLIMIENTO" : "NO_INCUMPLIMIENTO";
        var factores = (parsed.Factores ?? [])
            .Where(f => request.Vector.ContainsKey(f.CodigoCaracteristica) && f.Direccion is "AUMENTA_RIESGO" or "REDUCE_RIESGO" or "NEUTRO")
            .Select(f => new PredictionFactor(f.CodigoCaracteristica, f.Contribucion, f.Direccion)).ToList();

        return new RiskModelResponse(request.AnalisisId, request.ModeloVersionId, probabilidad, UmbralPredeterminado,
            clase, parsed.NivelRiesgo, (int)stopwatch.ElapsedMilliseconds, factores, Proveedor: "CLAUDE_TEMPORAL",
            ResumenRiesgo: string.IsNullOrWhiteSpace(parsed.ResumenRiesgo) ? null : parsed.ResumenRiesgo,
            Sugerencias: parsed.Sugerencias is { Count: > 0 } ? parsed.Sugerencias : null);
    }

    private sealed record ClaudeRiskResponse(
        [property: JsonPropertyName("probabilidad_incumplimiento")] double ProbabilidadIncumplimiento,
        [property: JsonPropertyName("nivel_riesgo")] string NivelRiesgo,
        [property: JsonPropertyName("resumen_riesgo")] string? ResumenRiesgo,
        [property: JsonPropertyName("sugerencias")] List<string>? Sugerencias,
        [property: JsonPropertyName("factores")] List<ClaudeRiskFactor> Factores);

    private sealed record ClaudeRiskFactor(
        [property: JsonPropertyName("codigo_caracteristica")] string CodigoCaracteristica,
        [property: JsonPropertyName("contribucion")] decimal Contribucion,
        [property: JsonPropertyName("direccion")] string Direccion);
}
