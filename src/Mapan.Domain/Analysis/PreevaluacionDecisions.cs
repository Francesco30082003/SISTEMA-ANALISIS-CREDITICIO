using System.Text.Json;

namespace Mapan.Domain.Analysis;

// Entradas primitivas (nada de tipos de Application) para construir el diccionario de campos que evalúa
// RuleEngine en la etapa de preevaluación — antes de solicitar documentos financieros, con lo que ya
// trajo la Investigación del Cliente (Fase 3). Campos "*_disponible" existen porque un campo ausente
// (p.ej. score cuando el cliente no tiene historial) haría que RuleEngine lance en vez de evaluar: las
// políticas deben verificar disponibilidad explícitamente en vez de que el sistema adivine un valor.
public sealed record PreevaluacionInputs(
    int? ScoreBuro,bool BuroDisponible,string BandaRiesgoBuro,decimal DeudaTotalBuro,decimal CuotaTotalBuro,
    int OperacionesVencidasBuro,int MaxDiasVencido3MesesBuro,decimal MontoDemandaJudicialBuro,decimal MontoCarteraCastigadaBuro,
    int? MoraActualMaxDiasBuro,
    bool JudicialDisponible,bool TieneProcesosJudiciales,int NumeroProcesosJudiciales,string GravedadJudicial,
    bool AvalDisponible,bool EsGaranteActivo,bool TieneMoraComoGarante,int OperacionesComoGarante,
    decimal MontoSolicitado,int PlazoSolicitadoMeses);

public sealed record PreevaluacionOutcome(string Accion,string Severidad,int Prioridad,string Motivo);

public static class PreevaluacionDecisions
{
    public static readonly string[] Resultados=["APTO","REQUIERE_EXCEPCION","NO_APTO"];
    // CONTINUAR: informativo, no cambia el resultado. ALERTA: queda registrada pero no bloquea.
    // REQUIERE_EXCEPCION: el resultado preliminar pasa a REQUIERE_EXCEPCION. BLOQUEAR: pasa a NO_APTO
    // (sigue pudiendo pedirse una excepción — ver §8 del pedido: "salvo autorización especial").
    public static readonly string[] Acciones=["CONTINUAR","ALERTA","REQUIERE_EXCEPCION","BLOQUEAR"];
    public static readonly string[] Severidades=["ROJO","AMARILLO","VERDE","INFO"];
    private static readonly Dictionary<string,int> AccionRank=new(){["CONTINUAR"]=0,["ALERTA"]=1,["REQUIERE_EXCEPCION"]=2,["BLOQUEAR"]=3};

    public static Dictionary<string,JsonElement> Fields(PreevaluacionInputs i)
    {
        var values=new Dictionary<string,object?>{
            ["score_buro"]=i.ScoreBuro??-1,["score_buro_disponible"]=i.BuroDisponible&&i.ScoreBuro.HasValue,
            ["banda_riesgo_buro"]=i.BandaRiesgoBuro,["buro_disponible"]=i.BuroDisponible,
            ["deuda_total_buro"]=i.DeudaTotalBuro,["cuota_total_buro"]=i.CuotaTotalBuro,
            ["operaciones_vencidas_buro"]=i.OperacionesVencidasBuro,["max_dias_vencido_3_meses_buro"]=i.MaxDiasVencido3MesesBuro,
            ["monto_demanda_judicial_buro"]=i.MontoDemandaJudicialBuro,["monto_cartera_castigada_buro"]=i.MontoCarteraCastigadaBuro,
            ["mora_actual_max_dias_buro"]=i.MoraActualMaxDiasBuro??-1,["mora_actual_disponible_buro"]=i.MoraActualMaxDiasBuro.HasValue,
            ["judicial_disponible"]=i.JudicialDisponible,["tiene_procesos_judiciales"]=i.TieneProcesosJudiciales,
            ["numero_procesos_judiciales"]=i.NumeroProcesosJudiciales,["gravedad_judicial"]=i.GravedadJudicial,
            ["aval_disponible"]=i.AvalDisponible,["es_garante_activo"]=i.EsGaranteActivo,
            ["tiene_mora_como_garante"]=i.TieneMoraComoGarante,["operaciones_como_garante"]=i.OperacionesComoGarante,
            ["monto_solicitado"]=i.MontoSolicitado,["plazo_solicitado_meses"]=i.PlazoSolicitadoMeses,
        };
        return values.ToDictionary(x=>x.Key,x=>JsonSerializer.SerializeToElement(x.Value));
    }

    // Forma deliberadamente distinta de RuleAction/ParseAction de RuleEngine: la etapa de análisis
    // financiero no debe poder verse afectada por cambios en este vocabulario, y viceversa.
    public static string ParseAction(string json)
    {
        if(json.Length>65536)throw new AnalysisConfigurationException("Acción demasiado grande.");
        try
        {
            using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=4});
            var root=doc.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||root.EnumerateObject().Any(p=>p.Name!="accion")||root.EnumerateObject().Count()!=1)
                throw new AnalysisConfigurationException("Estructura de acción de preevaluación no soportada.");
            var accion=root.TryGetProperty("accion",out var a)&&a.ValueKind==JsonValueKind.String?a.GetString():null;
            if(accion is null||!Acciones.Contains(accion))throw new AnalysisConfigurationException("Acción de preevaluación no soportada.");
            return accion;
        }
        catch(JsonException){throw new AnalysisConfigurationException("JSON de acción inválido.");}
    }

    // Gana la peor acción entre las reglas que aplicaron. Sin reglas aplicables => APTO.
    public static string Recommend(IReadOnlyList<PreevaluacionOutcome> outcomes)
    {
        if(outcomes.Count==0)return "APTO";
        var worst=outcomes.MaxBy(x=>AccionRank[x.Accion])!.Accion;
        return worst switch{"BLOQUEAR"=>"NO_APTO","REQUIERE_EXCEPCION"=>"REQUIERE_EXCEPCION",_=>"APTO"};
    }
}
