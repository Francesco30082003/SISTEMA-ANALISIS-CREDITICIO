namespace Mapan.Domain.Credito;

/// <summary>Desglose de la cuota estimada bajo amortización francesa (cuota fija).</summary>
public sealed record CreditoCalculoResultado(decimal CapitalMensual, decimal InteresMensual, decimal CuotaEstimada, decimal InteresTotal, decimal TotalAPagar);

public static class CreditoFrancesCalculator
{
    /// <summary>
    /// Sistema de amortización francesa (cuota fija): M = P·r·(1+r)^n / ((1+r)^n - 1), donde
    /// r es la tasa mensual (tasa anual del producto, normalizada de porcentaje a decimal y prorrateada /12)
    /// y n el plazo en meses. Tasa 0% (o ausente) degrada a capital fijo sin interés.
    /// CapitalMensual/InteresMensual reportan el desglose de la PRIMERA cuota (interés = monto·r; capital = cuota − interés):
    /// en amortización francesa la cuota es constante pero el reparto capital/interés varía cada mes conforme
    /// baja el saldo insoluto, así que no existe un "capital mensual" único para todo el plazo.
    /// </summary>
    public static CreditoCalculoResultado Calculate(decimal montoSolicitado, int plazoMeses, decimal? tasaInteresAnualPct)
    {
        if (montoSolicitado <= 0 || plazoMeses <= 0) return new(0m, 0m, 0m, 0m, montoSolicitado);
        var tasaDecimalMensual = (tasaInteresAnualPct ?? 0m) / 100m / 12m;
        if (tasaDecimalMensual <= 0m)
        {
            var cuotaPlana = Math.Round(montoSolicitado / plazoMeses, 2);
            return new(cuotaPlana, 0m, cuotaPlana, 0m, montoSolicitado);
        }
        var r = (double)tasaDecimalMensual;
        var factor = Math.Pow(1 + r, plazoMeses);
        var cuota = Math.Round((decimal)((double)montoSolicitado * r * factor / (factor - 1)), 2);
        var interesMensual = Math.Round(montoSolicitado * tasaDecimalMensual, 2);
        var capitalMensual = cuota - interesMensual;
        var totalAPagar = Math.Round(cuota * plazoMeses, 2);
        var interesTotal = totalAPagar - montoSolicitado;
        return new(capitalMensual, interesMensual, cuota, interesTotal, totalAPagar);
    }
}
