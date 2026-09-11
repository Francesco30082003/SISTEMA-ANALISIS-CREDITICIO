using Mapan.Application.Integrations;

namespace Mapan.Infrastructure.Integrations;

// No existe acceso a una API oficial del IESS para consulta programática por cédula en este entorno
// (investigado en Fase 1, igual disciplina que MockJudicialProvider: mock únicamente, sin scraping).
// Deja lista la interfaz para conectar la integración real el día que exista acceso.
public sealed class MockIessProvider : IVerificacionLaboralProvider
{
    public Task<VerificacionLaboralResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct)
    {
        var escenario = Math.Abs(numeroIdentificacion.GetHashCode()) % 4;
        var resultado = escenario switch
        {
            // Relación activa, empleador coincide con lo declarado por el cliente.
            0 => new VerificacionLaboralResultado(true,"Empleador registrado en IESS",new DateOnly(2021,3,15),480.00m,"ACTIVO"),
            // Relación activa, pero con OTRO empleador — posible inconsistencia con lo declarado.
            1 => new VerificacionLaboralResultado(true,"Empleador distinto al declarado por el cliente",new DateOnly(2023,8,1),425.00m,"ACTIVO"),
            // Cesante: relación terminó recientemente.
            2 => new VerificacionLaboralResultado(false,"Última relación laboral registrada",new DateOnly(2019,1,10),0m,"CESANTE"),
            // Sin ningún registro de aportación — coherente con independientes/informales.
            _ => new VerificacionLaboralResultado(false,null,null,null,"SIN_REGISTRO"),
        };
        return Task.FromResult(resultado);
    }
}
