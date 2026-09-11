using System.Text.Json;
using Mapan.Domain.Entities;

namespace Mapan.Domain.Analysis;

public sealed record RuleOutcome(string Severity,int Priority,string Result,string Reason);
public static class AnalysisDecisions
{
    public static readonly string[] Results=["CAPACIDAD_COMPATIBLE","CAPACIDAD_NO_COMPATIBLE","REVISION_ADICIONAL"];
    public static readonly string[] Severities=["ROJO","AMARILLO","VERDE","INFO"];
    public static readonly string[] ObligacionEstados=["VIGENTE","CANCELADA","CASTIGADA","REESTRUCTURADA"];
    private static readonly string[] ObligacionEstadosActivos=["VIGENTE","REESTRUCTURADA"];
    public static int ActiveDebtCount(IReadOnlyList<Obligacion> debts)=>debts.Count(x=>ObligacionEstadosActivos.Contains(x.Estado));
    public static decimal Money(decimal value)=>decimal.Round(value,2,MidpointRounding.AwayFromZero);
    public static decimal? Ratio(decimal? value)=>value.HasValue?decimal.Round(value.Value,8,MidpointRounding.AwayFromZero):null;
    public static bool IsMonthly(DateOnly start,DateOnly end)=>start.Day==1&&end==start.AddMonths(1).AddDays(-1);
    public static int? ActivityMonths(IReadOnlyList<ActividadEconomica> activities,DateOnly date)
    {
        var main=activities.Where(x=>x.EsPrincipal).ToArray();
        if(main.Length!=1||main[0].FechaInicio is not {} start||start>date)return null;
        return (date.Year-start.Year)*12+date.Month-start.Month-(date.Day<start.Day?1:0);
    }
    public static string Recommend(bool compatible,IReadOnlyList<RuleOutcome> outcomes)
    {
        if(outcomes.Any(x=>!Results.Contains(x.Result)||!Severities.Contains(x.Severity)))throw new AnalysisConfigurationException("Resultado o severidad de regla no soportado.");
        if(outcomes.Count==0)return compatible?Results[0]:Results[1];
        var best=outcomes.OrderBy(x=>Array.IndexOf(Severities,x.Severity)).ThenBy(x=>x.Priority).First();
        var results=outcomes.Where(x=>x.Severity==best.Severity&&x.Priority==best.Priority).Select(x=>x.Result).Distinct().ToArray();
        return results.Length==1?results[0]:Results[2];
    }
    public static SnapshotFinanciero Snapshot(FinancialResult r,Guid tenant,Guid analysis,DateTimeOffset now)=>new(){
        SnapshotFinancieroId=Guid.NewGuid(),EmpresaId=tenant,AnalisisId=analysis,IngresoTotalMensual=Money(r.Income),
        GastoTotalMensual=Money(r.Expenses),IngresoDisponible=Money(r.AvailableIncome),FactorCapacidad=decimal.Round(r.CapacityFactor,6,MidpointRounding.AwayFromZero),
        CapacidadNuevaCuota=Money(r.NewInstallmentCapacity),DeudaTotalActual=Money(r.Debt),CuotasActuales=Money(r.CurrentInstallments),
        CuotaNuevaEstimada=Money(r.NewInstallment),RatioEndeudamientoActual=Ratio(r.CurrentDebtRatio),RatioEndeudamientoPost=Ratio(r.PostDebtRatio),
        CuotaCompatible=Money(r.NewInstallment)<=Money(r.NewInstallmentCapacity),FechaCalculo=now};
    public static Dictionary<string,JsonElement> Fields(SnapshotFinanciero s,VectorCaracteristicas v)
    {
        var values=new Dictionary<string,object?>{
            ["ingreso_mensual"]=s.IngresoTotalMensual,["ingreso_total_mensual"]=s.IngresoTotalMensual,["gastos_mensuales"]=s.GastoTotalMensual,
            ["ingreso_disponible"]=s.IngresoDisponible,["factor_capacidad"]=s.FactorCapacidad,["capacidad_nueva_cuota"]=s.CapacidadNuevaCuota,
            ["deuda_total_actual"]=s.DeudaTotalActual,["cuotas_otras_deudas"]=s.CuotasActuales,["cuotas_actuales"]=s.CuotasActuales,
            ["cuota_estimada"]=s.CuotaNuevaEstimada,["cuota_compatible"]=s.CuotaCompatible,["ratio_actual"]=s.RatioEndeudamientoActual,["ratio_post"]=s.RatioEndeudamientoPost,
            ["monto_solicitado"]=v.MontoSolicitado,["plazo_meses"]=v.PlazoMeses,["max_dias_mora_historico"]=v.MaxDiasMoraHistorico,
            ["creditos_activos"]=v.CreditosActivos,["antiguedad_actividad_meses"]=v.AntiguedadActividadMeses,["estabilidad_ingresos_score"]=null,["historial_interno_score"]=null,
            ["deuda_sobre_ingreso"]=v.DeudaSobreIngreso,["cuota_sobre_ingreso"]=v.CuotaSobreIngreso,["monto_sobre_ingreso"]=v.MontoSobreIngreso,
            // Señales de Fases 3/6/7 (investigación, verificación documental, visita de negocio) — mismo
            // patrón "sentinel + *_disponible" que PreevaluacionDecisions.Fields: un campo ausente nunca
            // debe hacer que RuleEngine lance, la política que lo usa debe verificar disponibilidad primero.
            ["score_buro"]=v.ScoreBuro??-1,["score_buro_disponible"]=v.ScoreBuro.HasValue,
            ["mora_actual_max_dias_buro"]=v.MoraActualMaxDiasBuro??-1,["mora_actual_disponible_buro"]=v.MoraActualMaxDiasBuro.HasValue,
            ["tiene_procesos_judiciales"]=v.TieneProcesosJudiciales??false,["judicial_disponible"]=v.TieneProcesosJudiciales.HasValue,
            ["gravedad_judicial"]=v.GravedadJudicial??"NINGUNA",
            ["documentos_sospechosos"]=v.DocumentosSospechosos??0,
            ["visita_recomendacion"]=v.VisitaRecomendacion??"SIN_VISITA",["visita_disponible"]=v.VisitaRecomendacion!=null};
        return values.ToDictionary(x=>x.Key,x=>JsonSerializer.SerializeToElement(x.Value));
    }
}
