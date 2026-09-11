using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using Mapan.Application.Documentos;
using Mapan.Application.Integrations;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Integrations;

// Respaldo con IA (Anthropic Claude) para cuando la extracción determinista (texto/OCR + patrones,
// ver TextPdfDocumentExtractionService) no reconoció ningún campo: fotos de mala calidad, formatos no
// estandarizados, documentos sin texto extraíble. Nunca reemplaza la extracción determinista — solo
// se activa cuando esta no encontró nada — y solo acepta códigos de campo del catálogo conocido
// (DocumentFieldPatterns.All), para que ambas fuentes escriban a la misma tabla con el mismo contrato.
// Cada sugerencia sigue exigiendo confirmación humana (documentos.dato_validado) antes de aplicarse a
// la solicitud, igual que la ruta determinista. Ese par sugerencia-de-IA + corrección-humana es,
// además, el dataset con el que MAPAN podrá entrenar su propio modelo de extracción más adelante.
public sealed class HybridDocumentExtractionService(
    TextPdfDocumentExtractionService deterministic, MapanDbContext db, IFileStorage storage, IConfiguration configuration,
    ILogger<HybridDocumentExtractionService> logger)
    : IDocumentExtractionService
{
    private const string Modelo = "ANTHROPIC_CLAUDE_SONNET_5";
    private const string ExtractorVersion = "1";
    private static readonly HashSet<string> CamposConocidos = DocumentFieldPatterns.All.Select(p => p.CodigoCampo).ToHashSet();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IntegrationResult> ExtractAsync(Guid empresaId, Guid documentoVersionId, CancellationToken ct)
    {
        var deterministicResult = await deterministic.ExtractAsync(empresaId, documentoVersionId, ct);
        if (deterministicResult.Status is not ("NO_EXTRACTABLE_TEXT" or "SIN_CAMPOS_RECONOCIDOS" or "EXTRACCION_FALLIDA"))
            return deterministicResult;

        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) { logger.LogInformation("Sin Anthropic:ApiKey configurada; se omite el respaldo de IA."); return deterministicResult; }

        var version = await db.Set<DocumentoVersion>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId && x.DocumentoVersionId == documentoVersionId, ct);
        if (version is null || version.MimeType is not ("application/pdf" or "image/png" or "image/jpeg"))
            return deterministicResult;

        byte[] bytes;
        await using (var stream = await storage.OpenAsync(empresaId, version.AlmacenamientoUri, ct))
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            bytes = memory.ToArray();
        }

        List<ClaudeField>? campos;
        try { campos = await CallClaudeAsync(apiKey, bytes, version.MimeType, ct); }
        catch (Exception e) when (e is not OperationCanceledException) { logger.LogWarning(e, "Respaldo de IA (Claude) falló para el documento {DocumentoVersionId}", documentoVersionId); return deterministicResult; }
        if (campos is null || campos.Count == 0) { logger.LogInformation("Claude no reconoció campos en el documento {DocumentoVersionId}", documentoVersionId); return deterministicResult; }

        var now = DateTimeOffset.UtcNow;
        var found = 0;
        foreach (var campo in campos)
        {
            if (!CamposConocidos.Contains(campo.CodigoCampo)) continue; // nunca un código fuera del catálogo conocido.
            var parsed = ParseValor(campo.TipoDato, campo.Valor);
            if (parsed is null) continue; // no se pudo interpretar en el tipo declarado: se descarta, nunca se adivina.
            db.Add(new DatoExtraido
            {
                DatoExtraidoId = Guid.NewGuid(), EmpresaId = empresaId, DocumentoVersionId = documentoVersionId,
                CodigoCampo = campo.CodigoCampo, TipoDato = campo.TipoDato, ValorOriginal = campo.Valor,
                ValorTexto = parsed.Value.Texto, ValorNumerico = parsed.Value.Numero, ValorFecha = parsed.Value.Fecha,
                Confianza = Math.Clamp(campo.Confianza, 0m, 1m),
                ModeloExtraccion = Modelo, VersionExtractor = ExtractorVersion, FechaExtraccion = now,
            });
            found++;
        }
        if (found > 0) await db.SaveChangesAsync(ct);
        return new IntegrationResult(found > 0 ? "COMPLETADO" : "SIN_CAMPOS_RECONOCIDOS", null);
    }

    private static (string? Texto, decimal? Numero, DateOnly? Fecha)? ParseValor(string tipoDato, string valor) => tipoDato switch
    {
        "TEXTO" => (valor.Trim(), null, null),
        "NUMERO" => decimal.TryParse(valor.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? (null, n, null) : null,
        "FECHA" => DateOnly.TryParse(valor.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? (null, null, d) : null,
        _ => null,
    };

    private async Task<List<ClaudeField>> CallClaudeAsync(string apiKey, byte[] bytes, string mimeType, CancellationToken ct)
    {
        AnthropicClient client = new() { ApiKey = apiKey };
        var base64 = Convert.ToBase64String(bytes);
        ContentBlockParam fileBlock = mimeType == "application/pdf"
            ? new DocumentBlockParam { Source = new Base64PdfSource { Data = base64 } }
            : new ImageBlockParam { Source = new Base64ImageSource { Data = base64, MediaType = mimeType } };

        var codigos = DocumentFieldPatterns.All.Select(p => p.CodigoCampo).ToArray();
        var catalogo = string.Join(", ", DocumentFieldPatterns.All.Select(p => $"{p.CodigoCampo} ({p.TipoDato}) = {p.NombreCampo}"));
        var schema = new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                campos = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            codigo_campo = new { type = "string", @enum = codigos },
                            tipo_dato = new { type = "string", @enum = new[] { "TEXTO", "NUMERO", "FECHA" } },
                            valor = new { type = "string", description = "Valor tal como aparece en el documento, sin normalizar (fechas en formato yyyy-MM-dd)." },
                            confianza = new { type = "number", description = "0 a 1: qué tan seguro estás de que este valor es correcto." },
                        },
                        required = new[] { "codigo_campo", "tipo_dato", "valor", "confianza" },
                        additionalProperties = false,
                    },
                },
            }),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "campos" }),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-sonnet-5",
            MaxTokens = 4096,
            System = "Extraes datos financieros y laborales de documentos de clientes de una cooperativa de crédito ecuatoriana. " +
                     $"Reporta un campo solo si aparece explícitamente en el documento; nunca inventes ni infieras valores que no estén escritos. " +
                     $"Catálogo de campos posibles (usa exactamente estos códigos): {catalogo}.",
            OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = new List<ContentBlockParam> { fileBlock, new TextBlockParam { Text = "Extrae los campos del catálogo que reconozcas en este documento." } },
                },
            ],
        }, ct);

        var text = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text)) return [];
        var parsed = JsonSerializer.Deserialize<ClaudeExtractionResponse>(text, JsonOptions);
        return parsed?.Campos ?? [];
    }

    private sealed record ClaudeExtractionResponse([property: JsonPropertyName("campos")] List<ClaudeField> Campos);
    private sealed record ClaudeField(
        [property: JsonPropertyName("codigo_campo")] string CodigoCampo,
        [property: JsonPropertyName("tipo_dato")] string TipoDato,
        [property: JsonPropertyName("valor")] string Valor,
        [property: JsonPropertyName("confianza")] decimal Confianza);
}
