using Mapan.Application.Integrations;

namespace Mapan.Infrastructure.Integrations;

// No existe una API oficial/documentada del Consejo de la Judicatura del Ecuador para consulta
// automatizada por cédula en este entorno (se investigó antes de implementar: el portal público es
// solo de consulta manual por número de causa/proceso, no expone un servicio programático). Este mock
// deja lista la interfaz para conectar la integración real el día que exista acceso — sin scraping.
public sealed class MockJudicialProvider : IJudicialProvider
{
    public Task<JudicialResultado> ConsultarAsync(string numeroIdentificacion,CancellationToken ct)
    {
        var escenario = Math.Abs(numeroIdentificacion.GetHashCode()) % 4;
        var resultado = escenario switch
        {
            0 => new JudicialResultado(false,0,[]),
            1 => new JudicialResultado(true,1,[new("TRANSITO","EN_TRAMITE","BAJA")]),
            2 => new JudicialResultado(true,1,[new("CIVIL","EN_TRAMITE","MEDIA")]),
            _ => new JudicialResultado(true,2,[new("PENAL","EN_TRAMITE","ALTA"),new("CIVIL","RESUELTO","MEDIA")]),
        };
        return Task.FromResult(resultado);
    }
}
