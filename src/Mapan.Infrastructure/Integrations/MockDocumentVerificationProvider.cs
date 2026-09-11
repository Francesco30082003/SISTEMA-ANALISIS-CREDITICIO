using Mapan.Application.Integrations;

namespace Mapan.Infrastructure.Integrations;

// No hay un servicio real de verificación forense de documentos configurado en este entorno. El mock
// reproduce el resultado que un servicio así devolvería (VERIFICADO/SOSPECHOSO/INCONSISTENTE + motivos),
// determinístico por nombre de archivo para que las pruebas sean reproducibles.
public sealed class MockDocumentVerificationProvider : IDocumentVerificationProvider
{
    public Task<VerificacionDocumentoResultado> VerificarAsync(string tipoDocumento,string nombreArchivo,long tamanoBytes,CancellationToken ct)
    {
        var escenario = Math.Abs(nombreArchivo.GetHashCode()) % 6;
        var resultado = escenario switch
        {
            0 or 1 or 2 => new VerificacionDocumentoResultado("VERIFICADO",97.5m,[]),
            3 => new VerificacionDocumentoResultado("SOSPECHOSO",62.0m,["Metadatos del archivo no coinciden con la fecha de emisión declarada."]),
            4 => new VerificacionDocumentoResultado("SOSPECHOSO",58.5m,["Posible reescaneo de un documento ya procesado (patrón de compresión repetido)."]),
            _ => new VerificacionDocumentoResultado("INCONSISTENTE",31.0m,["El formato del número de documento no corresponde al tipo declarado.","Legibilidad insuficiente en campos clave."]),
        };
        return Task.FromResult(resultado);
    }
}
