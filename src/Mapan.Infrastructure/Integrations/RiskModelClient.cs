using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Integrations;
using Microsoft.Extensions.Configuration;

namespace Mapan.Infrastructure.Integrations;

public sealed class RiskModelClient(HttpClient http,IConfiguration configuration):IRiskModelClient
{
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web) {PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower};
    public async Task<RiskModelResponse> PredictAsync(RiskModelRequest request,CancellationToken ct)
    {
        if(!Uri.TryCreate(configuration["Ml:BaseUrl"],UriKind.Absolute,out var baseUri)
            ||baseUri.Scheme is not ("http" or "https"))throw ApplicationError.Configuration("Servicio ML no configurado.");
        using var response=await http.PostAsJsonAsync(new Uri(baseUri.ToString().TrimEnd('/')+"/predict"),request,JsonOptions,ct);
        if(response.StatusCode==HttpStatusCode.ServiceUnavailable)throw ApplicationError.Configuration("Modelo predictivo no configurado o no disponible.");
        response.EnsureSuccessStatusCode();
        var result=await response.Content.ReadFromJsonAsync<RiskModelResponse>(JsonOptions,ct)
            ??throw ApplicationError.Configuration("Respuesta ML vacía.");
        if(result.AnalisisId!=request.AnalisisId||result.ModeloVersionId!=request.ModeloVersionId
            ||result.ProbabilidadIncumplimiento is <0 or >1||result.UmbralUtilizado is <0 or >1
            ||result.TiempoInferenciaMs<0||result.ClasePredicha is not ("INCUMPLIMIENTO" or "NO_INCUMPLIMIENTO")
            ||result.NivelRiesgo is not (null or "BAJO" or "MEDIO" or "ALTO")||result.Factores is null
            ||result.Factores.Any(f=>string.IsNullOrWhiteSpace(f.CodigoCaracteristica)||f.Direccion is not ("AUMENTA_RIESGO" or "REDUCE_RIESGO" or "NEUTRO")))
            throw ApplicationError.Configuration("Respuesta ML incompatible con el contrato.");
        return result;
    }
}
