using System.Net;
using System.Net.Mail;
using Mapan.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Notifications;

// Sin Smtp:Host configurado, se registra en el log y no se lanza excepción — igual que
// UnconfiguredBureauProvider, una integración sin credenciales nunca debe romper el flujo que la usa.
public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation("Smtp:Host no configurado; alerta a {To} no enviada: {Subject}", to, subject);
            return;
        }
        var port = configuration.GetValue("Smtp:Port", 587);
        var user = configuration["Smtp:User"];
        var password = configuration["Smtp:Password"];
        var from = configuration["Smtp:From"] ?? user ?? "no-reply@mapan.local";
        var enableSsl = configuration.GetValue("Smtp:EnableSsl", true);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(user) ? null : new NetworkCredential(user, password),
        };
        using var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };
        try { await client.SendMailAsync(message, ct); }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "No se pudo enviar la alerta por correo a {To}", to);
        }
    }
}
