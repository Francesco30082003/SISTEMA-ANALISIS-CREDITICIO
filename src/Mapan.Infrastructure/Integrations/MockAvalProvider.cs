using Mapan.Application.Integrations;

namespace Mapan.Infrastructure.Integrations;

// Aval es un servicio privado sin credenciales configuradas en este entorno. Este mock permite probar
// el flujo (¿el cliente figura como garante activo en otras operaciones, y esas operaciones están al día?).
public sealed class MockAvalProvider : IAvalProvider
{
    public Task<AvalResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct)
    {
        var escenario = Math.Abs(numeroIdentificacion.GetHashCode()) % 3;
        var resultado = escenario switch
        {
            0 => new AvalResultado(false,0,false),
            1 => new AvalResultado(true,1,false),
            _ => new AvalResultado(true,2,true),
        };
        return Task.FromResult(resultado);
    }
}
