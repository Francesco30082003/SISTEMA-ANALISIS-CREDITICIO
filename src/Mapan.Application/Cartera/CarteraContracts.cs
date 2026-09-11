using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Cartera;

public sealed record MoraClienteDto(Guid PrestamoId, string NumeroPrestamo, Guid ClienteId, string ClienteNombre,
    string? Telefono, string? Correo, Guid SucursalId, string Sucursal, string Analista, int DiasMora, string Banda,
    decimal SaldoCapital, decimal CuotaExigible, DateOnly FechaCorte);

public interface ICarteraMoraRepository
{
    Task<IReadOnlyList<MoraClienteDto>> ListAsync(Guid empresaId, Guid member, CancellationToken ct);
    // Cualquier usuario cuyo rol tenga el permiso Mora:Notificar cuenta como destinatario de alta
    // gerencia — no se asume un nombre de rol fijo, cada empresa configura sus propios roles.
    Task<IReadOnlyList<string>> DestinatariosAlertaAsync(Guid empresaId, CancellationToken ct);
}

public sealed class CarteraMoraService(ICarteraMoraRepository repository, ICurrentTenant tenant, IPermissionChecker permissions, IEmailSender email)
{
    private const int UmbralAlertaDias = 30;

    public Task<IReadOnlyList<MoraClienteDto>> ListAsync(CancellationToken ct)
    {
        permissions.Require("Mora:Read");
        return repository.ListAsync(tenant.EmpresaId, tenant.UsuarioEmpresaId, ct);
    }

    public async Task<int> EnviarAlertaAsync(CancellationToken ct)
    {
        permissions.Require("Mora:Notificar");
        var criticos = (await repository.ListAsync(tenant.EmpresaId, tenant.UsuarioEmpresaId, ct)).Where(c => c.DiasMora >= UmbralAlertaDias).ToList();
        if (criticos.Count == 0) return 0;
        var destinatarios = await repository.DestinatariosAlertaAsync(tenant.EmpresaId, ct);
        if (destinatarios.Count == 0) return 0;
        var cuerpo = BuildBody(criticos);
        foreach (var correo in destinatarios)
            await email.SendAsync(correo, $"MAPAN: {criticos.Count} clientes en mora de {UmbralAlertaDias}+ días", cuerpo, ct);
        return destinatarios.Count;
    }

    private static string BandColor(int diasMora) => diasMora >= 90 ? "#b3261e" : diasMora >= 60 ? "#c65a00" : diasMora >= 30 ? "#a68000" : "#5b6b68";

    private static string BuildBody(IReadOnlyList<MoraClienteDto> clientes)
    {
        var filas = string.Join("", clientes.OrderByDescending(c => c.DiasMora).Select(c => $"""
            <tr>
              <td style="padding:8px 6px;border-bottom:1px solid #eef1f0;">
                <strong>{EmailTemplate.Escape(c.ClienteNombre)}</strong><br>
                <span style="color:#5b6b68;font-size:12.5px;">{EmailTemplate.Escape(c.Sucursal)} · analista {EmailTemplate.Escape(c.Analista)} · tel. {EmailTemplate.Escape(c.Telefono ?? "sin registrar")}</span>
              </td>
              <td style="padding:8px 6px;border-bottom:1px solid #eef1f0;text-align:right;white-space:nowrap;">
                <span style="display:inline-block;padding:2px 9px;border-radius:999px;background:{BandColor(c.DiasMora)}1a;color:{BandColor(c.DiasMora)};font-size:12.5px;font-weight:700;">{c.DiasMora} días</span>
              </td>
              <td style="padding:8px 6px;border-bottom:1px solid #eef1f0;text-align:right;white-space:nowrap;">{c.SaldoCapital:F2}</td>
            </tr>
            """));
        var body = $"""
            Se detectaron <strong>{clientes.Count} clientes</strong> con mora de {UmbralAlertaDias} días o más. Revisa la cartera completa en el módulo Mora de MAPAN.
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-top:16px;font-size:13.5px;">
              <tr>
                <th style="text-align:left;padding:6px;font-size:11px;color:#5b6b68;text-transform:uppercase;letter-spacing:0.5px;">Cliente</th>
                <th style="text-align:right;padding:6px;font-size:11px;color:#5b6b68;text-transform:uppercase;letter-spacing:0.5px;">Mora</th>
                <th style="text-align:right;padding:6px;font-size:11px;color:#5b6b68;text-transform:uppercase;letter-spacing:0.5px;">Saldo</th>
              </tr>
              {filas}
            </table>
            """;
        return EmailTemplate.Wrap("Cartera en mora", $"{clientes.Count} clientes requieren atención", body);
    }
}
