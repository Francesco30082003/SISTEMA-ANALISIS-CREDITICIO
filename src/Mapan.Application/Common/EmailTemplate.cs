using System.Net;

namespace Mapan.Application.Common;

// Plantilla HTML compartida para los correos del sistema (código de recuperación, alerta de mora).
// Solo dos remitentes la usan hoy — ver PasswordResetService y CarteraMoraService — así que vive
// aquí, en Application.Common, para que ambos la reutilicen sin depender de Infrastructure.
public static class EmailTemplate
{
    private const string Brand = "#12635d";
    private const string BrandDark = "#0b3f3c";
    private const string BgSoft = "#eef6f3";
    private const string TextMuted = "#5b6b68";

    // eyebrow: rótulo corto sobre el título (ej. "SEGURIDAD DE LA CUENTA"). bodyHtml: contenido ya
    // construido por el llamador (párrafos, tablas) — esta función solo aporta el marco visual.
    public static string Wrap(string eyebrow, string title, string bodyHtml, string? highlight = null)
    {
        var highlightBlock = highlight is null ? "" : $"""
            <div style="margin:20px 0;padding:18px 20px;background:{BgSoft};border-left:4px solid {Brand};border-radius:8px;">
              <span style="font-size:26px;font-weight:700;letter-spacing:2px;color:{BrandDark};font-family:'Courier New',monospace;">{highlight}</span>
            </div>
            """;
        return $"""
            <!doctype html>
            <html lang="es">
            <body style="margin:0;padding:0;background:#f4f6f5;font-family:Segoe UI,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f5;padding:32px 16px;">
                <tr><td align="center">
                  <table role="presentation" width="100%" style="max-width:560px;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 2px 10px rgba(11,63,60,0.08);">
                    <tr><td style="background:linear-gradient(135deg,{Brand},{BrandDark});padding:24px 32px;">
                      <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.5px;">MAPAN</span>
                      <div style="color:#cfe8e3;font-size:12px;margin-top:2px;">Plataforma de crédito cooperativo</div>
                    </td></tr>
                    <tr><td style="padding:32px 32px 8px 32px;">
                      <span style="color:{Brand};font-size:11px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;">{eyebrow}</span>
                      <h1 style="margin:6px 0 16px 0;font-size:21px;color:#1a2b29;">{title}</h1>
                      <div style="font-size:14.5px;line-height:1.6;color:#2c3937;">{bodyHtml}</div>
                      {highlightBlock}
                    </td></tr>
                    <tr><td style="padding:20px 32px 28px 32px;border-top:1px solid #eef1f0;">
                      <p style="margin:0;font-size:12px;color:{TextMuted};line-height:1.5;">
                        Este es un mensaje automático de MAPAN — no respondas a este correo.
                        Si no esperabas este mensaje, puedes ignorarlo con confianza.
                      </p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string Escape(string? value) => WebUtility.HtmlEncode(value ?? "");
}
