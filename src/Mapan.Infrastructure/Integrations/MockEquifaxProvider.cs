using Mapan.Application.Integrations;

namespace Mapan.Infrastructure.Integrations;

// Equifax es un servicio privado: sin credenciales/API oficial disponible en este entorno, este mock
// reproduce la FORMA real de un reporte de central de riesgo ecuatoriana (score, desglose de deuda por
// central SB/SEPS/SICOM, demanda judicial, cartera castigada, mora) para poder probar el flujo completo.
// Reemplazar por un EquifaxProvider real implementando la misma interfaz, sin tocar el resto del sistema.
public sealed class MockEquifaxProvider : ICreditBureauProvider
{
    public Task<BuroCreditoResultado> ConsultarAsync(string tipoIdentificacion,string numeroIdentificacion,CancellationToken ct)
    {
        var escenario = Math.Abs(numeroIdentificacion.GetHashCode()) % 8;
        var resultado = escenario switch
        {
            // Excelente: score alto, sin mora, sin novedades.
            0 => new BuroCreditoResultado(850,"BAJO",0.05m,2500m,120m,0,0,0m,0m,0,0,
                [new("ENTIDADES_SEPS",2500m,0m,0m,0m,2500m,120m)]),
            // Normal: buen score, deuda moderada al día.
            1 => new BuroCreditoResultado(735,"BAJO",0.10m,8200m,310m,0,0,0m,0m,0,0,
                [new("ENTIDADES_SEPS",8200m,0m,0m,0m,8200m,310m)]),
            // Mora: score deteriorado por atraso activo.
            2 => new BuroCreditoResultado(560,"ALTO",0.42m,15400m,540m,3,45,0m,0m,45,60,
                [new("SISTEMA_FINANCIERO_SB",9800m,5600m,0m,0m,15400m,540m)]),
            // Altamente endeudado: score aceptable pero deuda/cuota elevadas frente a lo típico.
            3 => new BuroCreditoResultado(640,"MEDIO",0.24m,38900m,1250m,2,12,0m,0m,12,20,
                [new("SISTEMA_FINANCIERO_SB",30000m,8900m,0m,0m,38900m,1250m)]),
            // Score bajo por causas serias (mora prolongada, sin castigo aún).
            4 => new BuroCreditoResultado(485,"ALTO",0.55m,6100m,210m,4,30,0m,0m,30,90,
                [new("SECTOR_COMERCIAL_SICOM",3200m,2900m,0m,0m,6100m,210m)]),
            // Score bajo SOLO por una obligación menor (candidato típico a excepción — ver Fase 4).
            5 => new BuroCreditoResultado(680,"MEDIO",0.184m,910m,60m,1,5,0m,0m,5,5,
                [new("ENTIDADES_SEPS",750m,160m,0m,0m,910m,60m)]),
            // Obligaciones castigadas: la señal más grave del reporte.
            6 => new BuroCreditoResultado(390,"ALTO",0.71m,4200m,0m,1,0,0m,4200m,0,180,
                [new("SISTEMA_FINANCIERO_SB",0m,0m,0m,4200m,4200m,0m)]),
            // Sin historial: cliente nuevo para el sistema financiero regulado.
            _ => new BuroCreditoResultado(null,"SIN_HISTORIAL",null,0m,0m,0,0,0m,0m,null,null,[]),
        };
        return Task.FromResult(resultado);
    }
}
