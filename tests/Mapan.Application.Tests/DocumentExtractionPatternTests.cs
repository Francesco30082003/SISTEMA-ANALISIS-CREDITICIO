using Mapan.Infrastructure.Integrations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace Mapan.Application.Tests;

public sealed class DocumentExtractionPatternTests
{
    private static byte[] BuildPdf(params string[] lines)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.Content().Column(col =>
            {
                col.Spacing(8);
                foreach (var line in lines) col.Item().Text(line);
            });
        })).GeneratePdf();
    }

    // Mirrors the label format DocumentFieldPatterns recognizes for income data — this is a
    // born-digital PDF (real text layer), not a scanned image, so no OCR is involved.
    internal static byte[] BuildSampleCertificadoIngresos() => BuildPdf(
        "CERTIFICADO DE INGRESOS",
        "Nombre: María Fernanda Rodríguez Ortiz",
        "Identificación: 0102030405",
        "Empleador: Textiles Andinos S.A.",
        "Cargo: Supervisora de Producción",
        "Fecha de ingreso: 2021-03-15",
        "Ingreso mensual: $1450.00",
        "Período desde: 2026-08-01",
        "Período hasta: 2026-08-31",
        "Certifico que la información aquí contenida es verídica.");

    internal static byte[] BuildSampleCertificadoDeuda() => BuildPdf(
        "CERTIFICADO DE DEUDA",
        "Institución: Cooperativa ZZTEST",
        "Saldo actual: $2300.50",
        "Cuota mensual: $180.00",
        "Días de mora: 0",
        "Estado: VIGENTE");

    // Fase 5: reporte de buró/Equifax cargado manualmente por el analista. Las etiquetas siguen el
    // formato observado en un reporte real de central de riesgo ecuatoriana (fecha dd/MM/yyyy incluida).
    internal static byte[] BuildSampleReporteBuro() => BuildPdf(
        "REPORTE DE BURO DE CREDITO",
        "Score Consultado: 735",
        "Saldo Deuda Total: $8200.00",
        "Cuota Total Mensual: $310.00",
        "Operaciones Vencidas: 0",
        "Monto Demanda Judicial: $0.00",
        "Monto Cartera Castigada: $0.00",
        "Dias de Mora Actual: 0",
        "Fecha de Corte: 31/07/2026");

    private static string ExtractText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);
        return string.Join('\n', document.GetPages().Select(TextPdfDocumentExtractionService.ExtractPageTextByLine));
    }

    private static Dictionary<string, ExtractedValue> MatchAll(string text)
    {
        var results = new Dictionary<string, ExtractedValue>();
        foreach (var pattern in DocumentFieldPatterns.All)
        {
            var match = pattern.Regex.Match(text);
            if (!match.Success) continue;
            var parsed = pattern.Parse(match.Groups["value"].Value);
            if (parsed is not null) results[pattern.CodigoCampo] = parsed;
        }
        return results;
    }

    [Fact]
    public void ExtractsEveryIncomeFieldFromTheSampleCertificate_RegardlessOfTipoDocumento()
    {
        var results = MatchAll(ExtractText(BuildSampleCertificadoIngresos()));

        Assert.Contains("Rodríguez", results["nombre_completo"].Texto);
        Assert.Equal("0102030405", results["identificacion"].Texto);
        Assert.Equal("Textiles Andinos S.A.", results["empleador"].Texto);
        Assert.Contains("Producción", results["cargo"].Texto);
        Assert.Equal(new DateOnly(2021, 3, 15), results["fecha_ingreso"].Fecha);
        Assert.Equal(1450.00m, results["ingreso_mensual"].Numero);
        Assert.Equal(new DateOnly(2026, 8, 1), results["periodo_desde"].Fecha);
        Assert.Equal(new DateOnly(2026, 8, 31), results["periodo_hasta"].Fecha);
        Assert.DoesNotContain("obligacion_saldo", results.Keys); // a income certificate has no debt labels.
    }

    [Fact]
    public void ExtractsObligacionFieldsFromADifferentDocumentShape()
    {
        var results = MatchAll(ExtractText(BuildSampleCertificadoDeuda()));

        Assert.Equal("Cooperativa ZZTEST", results["obligacion_institucion"].Texto);
        Assert.Equal(2300.50m, results["obligacion_saldo"].Numero);
        Assert.Equal(180.00m, results["obligacion_cuota"].Numero);
        Assert.Equal(0m, results["obligacion_mora"].Numero);
        Assert.Equal("VIGENTE", results["obligacion_estado"].Texto);
        Assert.DoesNotContain("ingreso_mensual", results.Keys); // a debt certificate has no income labels.
    }

    [Fact]
    public void ExtractsBuroReportFieldsFromASampleReport()
    {
        var results = MatchAll(ExtractText(BuildSampleReporteBuro()));

        Assert.Equal(735m, results["reporte_buro_score"].Numero);
        Assert.Equal(8200.00m, results["reporte_buro_deuda_total"].Numero);
        Assert.Equal(310.00m, results["reporte_buro_cuota_total"].Numero);
        Assert.Equal(0m, results["reporte_buro_operaciones_vencidas"].Numero);
        Assert.Equal(0m, results["reporte_buro_monto_demanda_judicial"].Numero);
        Assert.Equal(0m, results["reporte_buro_monto_cartera_castigada"].Numero);
        Assert.Equal(0m, results["reporte_buro_mora_actual_dias"].Numero);
        Assert.Equal(new DateOnly(2026, 7, 31), results["reporte_buro_fecha_reporte"].Fecha);
        Assert.DoesNotContain("obligacion_saldo", results.Keys); // no se confunde con las etiquetas de obligación del cliente.
    }

    [Fact]
    public void ADocumentWithNoneOfTheKnownLabelsMatchesNothing_NeverFabricated()
    {
        var results = MatchAll(ExtractText(BuildPdf("Este es un documento cualquiera sin campos reconocidos.")));
        Assert.Empty(results);
    }

    // Simulates a photographed/scanned document: draws text onto a bitmap (no PDF text layer at
    // all), saves it as PNG, and runs it through the SAME OCR path TextPdfDocumentExtractionService
    // uses — real Tesseract, not a mock. Proves the OCR wiring (native binaries + spa.traineddata)
    // actually works, not just that PdfPig text extraction works.
    internal static byte[] BuildSampleIncomePhoto()
    {
        using var bitmap = new System.Drawing.Bitmap(900, 260);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(System.Drawing.Color.White);
            using var font = new System.Drawing.Font("Arial", 22, System.Drawing.FontStyle.Regular);
            var lines = new[] { "Empleador: Panaderia El Trigal Cia Ltda", "Ingreso mensual: 980.00" };
            var y = 20;
            foreach (var line in lines) { g.DrawString(line, font, System.Drawing.Brushes.Black, 20, y); y += 70; }
        }
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    [Fact]
    public void RealOcrExtractsFieldsFromAPhotographedDocument()
    {
        var text = ExtractImageTextForTest(BuildSampleIncomePhoto());
        var results = MatchAll(text);
        Assert.True(results.ContainsKey("ingreso_mensual") || results.ContainsKey("empleador"),
            "OCR debería reconocer al menos un campo del texto dibujado. Texto OCR obtenido: " + text);
    }

    private static string ExtractImageTextForTest(byte[] pngBytes)
    {
        var tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        using var engine = new Tesseract.TesseractEngine(tessDataPath, "spa", Tesseract.EngineMode.Default);
        using var image = Tesseract.Pix.LoadFromMemory(pngBytes);
        using var page = engine.Process(image);
        return page.GetText();
    }
}
