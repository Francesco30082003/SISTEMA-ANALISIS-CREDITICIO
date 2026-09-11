using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Mapan.Application.Security;

public sealed record ForgotPasswordInput([Required, MaxLength(200)] string Identificador);
public sealed record ResetPasswordInput([Required, MaxLength(200)] string Identificador,
    [Required, MaxLength(10)] string Codigo, [Required, MinLength(12), MaxLength(1024)] string NuevaClave);

// Código de un solo uso en caché de memoria (15 min, máx. 5 intentos) — no requiere una tabla nueva:
// vive solo mientras el proceso está arriba, suficiente para un solo servidor y una ventana tan corta.
// Nunca revela si un identificador existe o no, para no facilitar enumeración de usuarios.
public sealed class PasswordResetService(IAuthenticationRepository repository, IPasswordService passwords,
    IEmailSender email, IMemoryCache cache)
{
    private const int ExpiracionMinutos = 15;
    private const int IntentosMaximos = 5;
    private sealed record PendingReset(string Codigo, int Intentos);
    private static string CacheKey(Guid usuarioId) => $"pwdreset:{usuarioId}";

    public async Task RequestAsync(ForgotPasswordInput input, CancellationToken ct)
    {
        if (!Validator.TryValidateObject(input, new ValidationContext(input), [], true)) return;
        var user = await repository.FindUserAsync(input.Identificador.Trim(), ct);
        if (user is null || user.Estado != "ACTIVO") return;
        var codigo = Random.Shared.Next(0, 1_000_000).ToString("D6");
        cache.Set(CacheKey(user.UsuarioId), new PendingReset(codigo, 0), TimeSpan.FromMinutes(ExpiracionMinutos));
        var body = EmailTemplate.Wrap("Seguridad de la cuenta", "Restablece tu contraseña",
            $"Recibimos una solicitud para restablecer la contraseña de tu cuenta MAPAN. " +
            $"Usa el siguiente código en la pantalla de restablecimiento — expira en {ExpiracionMinutos} minutos.",
            EmailTemplate.Escape(codigo));
        await email.SendAsync(user.Correo, "MAPAN: código para restablecer tu contraseña", body, ct);
    }

    public async Task ResetAsync(ResetPasswordInput input, CancellationToken ct)
    {
        if (!Validator.TryValidateObject(input, new ValidationContext(input), [], true))
            throw new ApplicationError(400, "VALIDATION", "Datos inválidos.");
        var user = await repository.FindUserAsync(input.Identificador.Trim(), ct);
        var key = user is null ? null : CacheKey(user.UsuarioId);
        if (user is null || !cache.TryGetValue(key!, out PendingReset? pending) || pending is null)
            throw new ApplicationError(400, "VALIDATION", "Código inválido o expirado.");
        if (pending.Intentos >= IntentosMaximos)
        {
            cache.Remove(key!);
            throw new ApplicationError(400, "VALIDATION", "Demasiados intentos. Solicita un nuevo código.");
        }
        if (pending.Codigo != input.Codigo.Trim())
        {
            cache.Set(key!, pending with { Intentos = pending.Intentos + 1 }, TimeSpan.FromMinutes(ExpiracionMinutos));
            throw new ApplicationError(400, "VALIDATION", "Código inválido o expirado.");
        }
        cache.Remove(key!);
        await repository.UpdatePasswordHashAsync(user.UsuarioId, passwords.Hash(user, input.NuevaClave), ct);
    }
}
