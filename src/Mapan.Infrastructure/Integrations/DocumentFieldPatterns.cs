using System.Globalization;
using System.Text.RegularExpressions;

namespace Mapan.Infrastructure.Integrations;

// Deterministic text-pattern extraction, not OCR: works only on born-digital PDFs that already
// carry a text layer (e.g. a certificate generated from a system), keyed by tipo_documento.
// A scanned/photographed document has no text layer and is honestly reported as such — never guessed.
public sealed record ExtractedValue(string? Texto, decimal? Numero, DateOnly? Fecha, bool? Booleano);
public sealed record FieldPattern(string CodigoCampo, string NombreCampo, string TipoDato, Regex Regex, Func<string, ExtractedValue?> Parse);

public static class DocumentFieldPatterns
{
    private static ExtractedValue Text(string raw) => new(raw.Trim(), null, null, null);
    private static ExtractedValue? Number(string raw) =>
        decimal.TryParse(raw.Replace(",", "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? new ExtractedValue(null, n, null, null) : null;
    private static ExtractedValue? Date(string raw) =>
        DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? new ExtractedValue(null, null, d, null) : null;
    // Reportes ecuatorianos (buró/Equifax) suelen usar dd/MM/yyyy en vez del yyyy-MM-dd que emiten los
    // certificados generados por sistema; se acepta cualquiera de los dos sin adivinar el que falte.
    private static ExtractedValue? DateFlexible(string raw)
    {
        var s = raw.Trim();
        foreach (var format in new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy" })
            if (DateOnly.TryParseExact(s, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return new ExtractedValue(null, null, d, null);
        return null;
    }

    // Not keyed by tipo_documento: the analyst's free-text label is unreliable (case, wording), and
    // these labels are specific enough that trying all of them against any digital-text document is
    // safe — a document that doesn't contain "Ingreso mensual:" simply won't match that pattern.
    public static readonly IReadOnlyList<FieldPattern> All = new List<FieldPattern>
    {
        // Datos laborales / de ingreso (ej. certificado de ingresos, rol de pagos, carta laboral).
        new("nombre_completo", "Nombre completo", "TEXTO", new Regex(@"Nombre:\s*(?<value>.+)", RegexOptions.IgnoreCase), Text),
        new("identificacion", "Identificación", "TEXTO", new Regex(@"Identificaci[oó]n:\s*(?<value>[\w-]+)", RegexOptions.IgnoreCase), Text),
        new("empleador", "Empleador", "TEXTO", new Regex(@"Empleador:\s*(?<value>.+)", RegexOptions.IgnoreCase), Text),
        new("cargo", "Cargo", "TEXTO", new Regex(@"Cargo:\s*(?<value>.+)", RegexOptions.IgnoreCase), Text),
        new("fecha_ingreso", "Fecha de ingreso", "FECHA", new Regex(@"Fecha de ingreso:\s*(?<value>\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase), Date),
        new("ingreso_mensual", "Ingreso mensual", "NUMERO", new Regex(@"Ingreso mensual:\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("periodo_desde", "Período desde", "FECHA", new Regex(@"Per[ií]odo desde:\s*(?<value>\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase), Date),
        new("periodo_hasta", "Período hasta", "FECHA", new Regex(@"Per[ií]odo hasta:\s*(?<value>\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase), Date),
        // Obligaciones declaradas por el cliente (ej. certificado de deuda, tabla de amortización,
        // estado de cuenta). El futuro cruce con Equifax/Aval usa credito.obligacion_fuente; esto
        // solo cubre la mitad "documento entregado por el cliente" de ese híbrido.
        new("obligacion_institucion", "Institución de la obligación", "TEXTO", new Regex(@"Instituci[oó]n:\s*(?<value>.+)", RegexOptions.IgnoreCase), Text),
        new("obligacion_saldo", "Saldo de la obligación", "NUMERO", new Regex(@"Saldo(?: actual)?:\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("obligacion_cuota", "Cuota de la obligación", "NUMERO", new Regex(@"Cuota mensual:\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("obligacion_mora", "Días de mora de la obligación", "NUMERO", new Regex(@"D[ií]as de mora:\s*(?<value>\d+)", RegexOptions.IgnoreCase), Number),
        new("obligacion_estado", "Estado de la obligación", "TEXTO", new Regex(@"Estado(?: de la obligaci[oó]n)?:\s*(?<value>VIGENTE|CANCELADA|CASTIGADA|REESTRUCTURADA)", RegexOptions.IgnoreCase), Text),
        // Reportes de buró de crédito / Equifax cargados manualmente (Fase 5, §5 del pedido). Etiquetas
        // amplias a propósito: cada cooperativa puede recibir el reporte con encabezados ligeramente
        // distintos según el proveedor. Si el texto no calza, el respaldo con IA (misma lista de
        // códigos, ver HybridDocumentExtractionService) sigue intentándolo — nunca se inventa un valor.
        new("reporte_buro_score", "Score de buró", "NUMERO", new Regex(@"(?:Score(?:\s+Consultado)?|MATRIZ\s+DUAL)\s*[:\-]?\s*(?<value>\d{2,3})\b", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_deuda_total", "Deuda total reportada", "NUMERO", new Regex(@"(?:Saldo Deuda(?: Total)?|Deuda Total)\s*:?\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_cuota_total", "Cuota total mensual reportada", "NUMERO", new Regex(@"Cuota(?: Total)? Mensual\s*:?\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_operaciones_vencidas", "Operaciones vencidas reportadas", "NUMERO", new Regex(@"(?:Operaciones Vencidas|Total Operaciones Impagos)\s*:?\s*(?<value>\d+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_monto_demanda_judicial", "Monto en demanda judicial", "NUMERO", new Regex(@"(?:Monto Demanda Judicial|Saldo Total Operaciones Demanda Judicial)\s*:?\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_monto_cartera_castigada", "Monto en cartera castigada", "NUMERO", new Regex(@"(?:Monto Cartera Castigada|Saldo Total Operaciones Cartera Castigada)\s*:?\s*\$?\s*(?<value>[\d.,]+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_mora_actual_dias", "Días de mora actual reportados", "NUMERO", new Regex(@"(?:D[ií]as de Mora Actual|Mora Actual)\s*(?:\(d[ií]as\))?\s*:?\s*(?<value>\d+)", RegexOptions.IgnoreCase), Number),
        new("reporte_buro_fecha_reporte", "Fecha del reporte", "FECHA", new Regex(@"Fecha (?:de )?Corte\s*:?\s*(?<value>\d{4}-\d{2}-\d{2}|\d{1,2}[/-]\d{1,2}[/-]\d{4})", RegexOptions.IgnoreCase), DateFlexible),
    };
}
