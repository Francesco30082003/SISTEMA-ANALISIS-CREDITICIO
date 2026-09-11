using Mapan.Application.Documentos;
using Mapan.Application.Integrations;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tesseract;
using UglyToad.PdfPig;

namespace Mapan.Infrastructure.Integrations;

// Two real, deterministic-as-possible extraction paths — neither ever fabricates a value:
//   - application/pdf with a text layer: exact text extraction (PdfPig), effectively 100% accurate.
//   - image/png, image/jpeg (a photographed or scanned document): real OCR (Tesseract, Spanish
//     trained data, local — no cloud credentials, no cost per document). Accuracy depends on photo
//     quality (lighting, skew, resolution); this is inherent to OCR, not specific to this pipeline.
// A scanned PDF (image-only, no text layer) is not rasterized/OCR'd yet — that needs a separate PDF
// rendering step and is a follow-up, not built here. It is honestly reported as NO_EXTRACTABLE_TEXT.
// Every pattern in DocumentFieldPatterns.All is tried regardless of the analyst's free-text
// "tipo de documento", since that label is unreliable. Extracted values always land in
// documentos.dato_extraido as suggestions — a human must still confirm them before they count.
public sealed class TextPdfDocumentExtractionService(MapanDbContext db, IFileStorage storage) : IDocumentExtractionService
{
    private const string ExtractorVersion = "1";
    private const string ModeloPdf = "MAPAN_TEXTO_PDF_REGEX";
    private const string ModeloOcr = "MAPAN_OCR_TESSERACT_SPA";
    private static readonly string TessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

    public async Task<IntegrationResult> ExtractAsync(Guid empresaId, Guid documentoVersionId, CancellationToken ct)
    {
        var version = await db.Set<DocumentoVersion>().AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == empresaId && x.DocumentoVersionId == documentoVersionId, ct);
        if (version is null) return new IntegrationResult("NOT_CONFIGURED", null);

        string text; string modelo;
        try
        {
            await using var stream = await storage.OpenAsync(empresaId, version.AlmacenamientoUri, ct);
            switch (version.MimeType)
            {
                case "application/pdf": text = ExtractPdfText(stream); modelo = ModeloPdf; break;
                case "image/png" or "image/jpeg": text = ExtractImageTextViaOcr(stream); modelo = ModeloOcr; break;
                default: return new IntegrationResult("NOT_CONFIGURED", null);
            }
        }
        catch
        {
            return new IntegrationResult("EXTRACCION_FALLIDA", null);
        }
        if (string.IsNullOrWhiteSpace(text)) return new IntegrationResult("NO_EXTRACTABLE_TEXT", null);

        var now = DateTimeOffset.UtcNow;
        var found = 0;
        foreach (var pattern in DocumentFieldPatterns.All)
        {
            var match = pattern.Regex.Match(text);
            if (!match.Success) continue;
            var raw = match.Groups["value"].Value;
            var parsed = pattern.Parse(raw);
            if (parsed is null) continue; // couldn't confidently parse into the expected type: skip, never fabricate a guess.
            db.Add(new DatoExtraido
            {
                DatoExtraidoId = Guid.NewGuid(), EmpresaId = empresaId, DocumentoVersionId = documentoVersionId,
                CodigoCampo = pattern.CodigoCampo, NombreCampo = pattern.NombreCampo, TipoDato = pattern.TipoDato,
                ValorOriginal = raw.Trim(), ValorTexto = parsed.Texto, ValorNumerico = parsed.Numero, ValorFecha = parsed.Fecha, ValorBooleano = parsed.Booleano,
                Confianza = modelo == ModeloOcr ? null : 1m, // OCR confidence is not a meaningful single number here; never invent one.
                ModeloExtraccion = modelo, VersionExtractor = ExtractorVersion, FechaExtraccion = now,
            });
            found++;
        }
        if (found > 0) await db.SaveChangesAsync(ct);
        return new IntegrationResult(found > 0 ? "COMPLETADO" : "SIN_CAMPOS_RECONOCIDOS", null);
    }

    private static string ExtractPdfText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        return string.Join('\n', document.GetPages().Select(ExtractPageTextByLine));
    }

    private static string ExtractImageTextViaOcr(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        using var engine = new TesseractEngine(TessDataPath, "spa", EngineMode.Default);
        using var image = Pix.LoadFromMemory(memory.ToArray());
        using var page = engine.Process(image);
        return page.GetText();
    }

    // page.Text alone concatenates words in reading order without a reliable line break, which
    // merges adjacent lines (e.g. "0102030405Empleador"). Group words by their vertical position
    // instead, to reconstruct actual lines before matching label patterns against them.
    public static string ExtractPageTextByLine(UglyToad.PdfPig.Content.Page page) => string.Join('\n',
        page.GetWords()
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
            .OrderByDescending(g => g.Key)
            .Select(g => string.Join(' ', g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))));
}
