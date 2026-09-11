using System.Globalization;
using System.Text.Json;
using Mapan.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mapan.Application.Analysis;

public static class AnalysisReport
{
    static AnalysisReport() => QuestPDF.Settings.License = LicenseType.Community;

    private const string Primary = "#12635d";
    private const string Line = "#dddddd";
    private const string Muted = "#637789";

    public static byte[] Render(AnalysisDetail detail)
    {
        var s = detail.Snapshot ?? throw new ApplicationError(422, "REPORT_UNAVAILABLE", "Esta ejecución no tiene un informe completado.");
        var r = detail.Recomendacion ?? throw new ApplicationError(422, "REPORT_UNAVAILABLE", "No hay recomendación para esta ejecución.");
        string E(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "Sin dato";
        string C(string key) => detail.Contexto is { } c && c.TryGetProperty(key, out var value)
            ? (value.ValueKind == JsonValueKind.String ? value.GetString() ?? "Sin dato" : value.ToString())
            : "Sin dato";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                page.Header().Column(col =>
                {
                    col.Item().Text("MAPAN / ANÁLISIS CREDITICIO").FontSize(9).SemiBold().FontColor(Primary);
                    col.Item().PaddingTop(2).Text("Informe de apoyo a la decisión").FontSize(20).Bold();
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Primary);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Element(c => KeyValueTable(c, new (string, string?)[]
                    {
                        ("Empresa", C("empresa")), ("Cliente", C("cliente")), ("Identificación", C("identificacion")),
                        ("Solicitud", C("solicitud")), ("Producto", C("producto")),
                        ("Monto", C("monto")), ("Plazo", C("plazo")), ("Moneda", C("moneda")), ("Destino", C("destino")),
                        ("Política", C("politica")), ("Versión de política", C("versionPolitica")), ("Versión de modelo", C("versionModelo")),
                        ("Fecha", C("fecha")), ("Ejecución", "#" + detail.Analisis.NumeroEjecucion), ("Estado", detail.Analisis.Estado),
                    }));

                    Section(col, "Capacidad de pago");
                    col.Item().Element(c => KeyValueTable(c, new (string, string?)[]
                    {
                        ("Ingreso mensual", E(s.IngresoTotalMensual)), ("Gastos mensuales", E(s.GastoTotalMensual)),
                        ("Ingreso disponible", E(s.IngresoDisponible)), ("Factor aplicado", E(s.FactorCapacidad)),
                        ("Capacidad nueva cuota", E(s.CapacidadNuevaCuota)), ("Cuota propuesta", E(s.CuotaNuevaEstimada)),
                        ("Cuota compatible", s.CuotaCompatible ? "Sí" : "No"),
                    }));

                    Section(col, "Endeudamiento");
                    col.Item().Element(c => KeyValueTable(c, new (string, string?)[]
                    {
                        ("Deuda actual", E(s.DeudaTotalActual)), ("Cuotas actuales", E(s.CuotasActuales)),
                        ("Ratio actual", E(s.RatioEndeudamientoActual)), ("Ratio posterior", E(s.RatioEndeudamientoPost)),
                    }));

                    Section(col, "Obligaciones declaradas");
                    if (detail.Obligaciones.Count == 0) col.Item().Text("Sin obligaciones registradas.").FontColor(Muted);
                    else col.Item().Element(c => ObligacionesTable(c, detail.Obligaciones));

                    Section(col, "Documentos");
                    if (detail.Documentos.Count == 0) col.Item().Text("Sin documentos asociados.").FontColor(Muted);
                    else col.Item().Element(c => DocumentosTable(c, detail.Documentos));

                    Section(col, "Reglas y alertas");
                    if (detail.Alertas.Count == 0) col.Item().Text("No se activaron alertas en esta ejecución.").FontColor(Muted);
                    foreach (var a in detail.Alertas)
                        col.Item().Text(t =>
                        {
                            t.Span(a.Nivel + " · " + a.Titulo).SemiBold();
                            t.Line(a.Descripcion);
                            t.Span("Fuente: " + a.Codigo + " · " + (a.Resuelta ? "Resuelta" : "Pendiente")).FontSize(8).FontColor(Muted);
                        });
                    if (detail.Contexto is { } context && context.TryGetProperty("reglas", out var rules))
                        foreach (var rule in rules.EnumerateArray())
                            col.Item().Text(rule.GetProperty("nombre").GetString() + " · " + (rule.GetProperty("aplica").GetBoolean() ? "Aplicó" : "No aplicó") + " · " + rule.GetProperty("resultado").GetString()).FontSize(8.5f);

                    Section(col, "Modelo predictivo");
                    col.Item().Text(C("mlStatus"));
                    if (detail.Prediccion is { } prediction)
                        col.Item().Text("Probabilidad: " + E(prediction.ProbabilidadIncumplimiento) + " · Nivel: " + E(prediction.NivelRiesgo));
                    if (C("resumenRiesgo") is { } resumenRiesgo && resumenRiesgo != "Sin dato")
                        col.Item().Text(resumenRiesgo).Italic();
                    if (detail.Contexto is { } ctx && ctx.TryGetProperty("sugerenciasRiesgo", out var sugerencias) && sugerencias.ValueKind == JsonValueKind.Array && sugerencias.GetArrayLength() > 0)
                    {
                        col.Item().Text("Sugerencias para la cooperativa:").SemiBold().FontSize(9.5f);
                        foreach (var s in sugerencias.EnumerateArray())
                            col.Item().Text("• " + s.GetString()).FontSize(9);
                    }

                    col.Item().Background("#eef6f3").Padding(12).Column(rc =>
                    {
                        rc.Spacing(4);
                        rc.Item().Text("Recomendación del sistema").FontSize(11).Bold();
                        rc.Item().Text(r.CodigoRecomendacion).SemiBold();
                        rc.Item().Text(r.Resumen ?? "Sin resumen.");
                        rc.Item().Text("Requiere revisión humana. No constituye aprobación ni rechazo.").FontSize(8.5f).Italic();
                    });
                });

                page.Footer().PaddingTop(10).Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Primary);
                    col.Item().PaddingTop(6).Text("Este informe constituye una herramienta de apoyo al análisis crediticio. La decisión final corresponde al personal autorizado de la entidad.").FontSize(8).FontColor(Muted);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Section(ColumnDescriptor col, string title) =>
        col.Item().Text(title).FontSize(13).Bold().FontColor(Primary);

    private static void KeyValueTable(IContainer container, (string Key, string? Value)[] rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(140);
                columns.RelativeColumn();
            });
            foreach (var (key, value) in rows)
            {
                table.Cell().Element(LabelCell).Text(key).SemiBold();
                table.Cell().Element(ValueCell).Text(value ?? "Sin dato");
            }
        });
    }

    private static void ObligacionesTable(IContainer container, IReadOnlyList<ObligacionResumen> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
            table.Header(header =>
            {
                foreach (var title in new[] { "Institución", "Tipo", "Saldo", "Cuota", "Mora", "Estado" })
                    header.Cell().Element(HeaderCell).Text(title);
            });
            foreach (var o in items)
            {
                table.Cell().Element(ValueCell).Text(o.Institucion ?? "Sin dato");
                table.Cell().Element(ValueCell).Text(o.TipoObligacion ?? "Sin dato");
                table.Cell().Element(ValueCell).Text(o.SaldoActual?.ToString(CultureInfo.InvariantCulture) ?? "Sin dato");
                table.Cell().Element(ValueCell).Text(o.CuotaMensual?.ToString(CultureInfo.InvariantCulture) ?? "Sin dato");
                table.Cell().Element(ValueCell).Text(o.DiasMoraActual + " días");
                table.Cell().Element(ValueCell).Text(o.Estado ?? "Sin dato");
            }
        });
    }

    private static void DocumentosTable(IContainer container, IReadOnlyList<DocumentoResumen> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(3); columns.RelativeColumn(); columns.RelativeColumn(2); columns.RelativeColumn(); columns.RelativeColumn(2); });
            table.Header(header =>
            {
                foreach (var title in new[] { "Tipo", "Archivo", "Versión", "Uso", "Obligatorio", "Estado" })
                    header.Cell().Element(HeaderCell).Text(title);
            });
            foreach (var d in items)
            {
                table.Cell().Element(ValueCell).Text(d.TipoDocumento);
                table.Cell().Element(ValueCell).Text(d.NombreArchivo);
                table.Cell().Element(ValueCell).Text(d.NumeroVersion.ToString());
                table.Cell().Element(ValueCell).Text(d.UsoDocumento ?? "Sin dato");
                table.Cell().Element(ValueCell).Text(d.Obligatorio ? "Sí" : "No");
                table.Cell().Element(ValueCell).Text(d.EstadoAsociacion);
            }
        });
    }

    private static IContainer LabelCell(IContainer container) => container.BorderBottom(1).BorderColor(Line).PaddingVertical(4).PaddingRight(8);
    private static IContainer ValueCell(IContainer container) => container.BorderBottom(1).BorderColor(Line).PaddingVertical(4);
    private static IContainer HeaderCell(IContainer container) => container.BorderBottom(1).BorderColor(Primary).PaddingVertical(4);
}
