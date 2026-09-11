using System.Text.Json;
using Mapan.Domain.Analysis;

namespace Mapan.Domain.Tests;

public sealed class PreevaluacionDecisionsTests
{
    private static PreevaluacionInputs Inputs(int? score=750,bool buroDisponible=true,bool tieneProcesos=false,string gravedad="NINGUNA") =>
        new(score,buroDisponible,"BAJO",0m,0m,0,0,0m,0m,0,true,tieneProcesos,tieneProcesos?1:0,gravedad,true,false,false,0,10000m,12);

    [Fact] public void NoMatchedRulesIsApto() => Assert.Equal("APTO",PreevaluacionDecisions.Recommend([]));
    [Fact] public void WorstMatchedActionWins()
    {
        var outcomes=new[]{new PreevaluacionOutcome("ALERTA","AMARILLO",100,"aviso"),new PreevaluacionOutcome("BLOQUEAR","ROJO",50,"grave")};
        Assert.Equal("NO_APTO",PreevaluacionDecisions.Recommend(outcomes));
    }
    [Fact] public void RequiereExcepcionWithoutBloquearStaysRequiereExcepcion()
    {
        var outcomes=new[]{new PreevaluacionOutcome("CONTINUAR","INFO",100,"ok"),new PreevaluacionOutcome("REQUIERE_EXCEPCION","AMARILLO",50,"score bajo")};
        Assert.Equal("REQUIERE_EXCEPCION",PreevaluacionDecisions.Recommend(outcomes));
    }
    [Fact] public void OnlyContinuarOrAlertaStaysApto()
    {
        var outcomes=new[]{new PreevaluacionOutcome("CONTINUAR","INFO",100,"ok"),new PreevaluacionOutcome("ALERTA","AMARILLO",50,"aviso")};
        Assert.Equal("APTO",PreevaluacionDecisions.Recommend(outcomes));
    }
    [Fact] public void ParseActionAcceptsKnownActions() => Assert.Equal("BLOQUEAR",PreevaluacionDecisions.ParseAction("""{"accion":"BLOQUEAR"}"""));
    [Fact] public void ParseActionRejectsUnknownAction() => Assert.Throws<AnalysisConfigurationException>(()=>PreevaluacionDecisions.ParseAction("""{"accion":"EXEC"}"""));
    [Fact] public void ParseActionRejectsExtraProperties() => Assert.Throws<AnalysisConfigurationException>(()=>PreevaluacionDecisions.ParseAction("""{"accion":"CONTINUAR","resultado":"X"}"""));
    [Fact]
    public void FieldsExposeAvailabilityGuardsForNullableScore()
    {
        var fields=PreevaluacionDecisions.Fields(Inputs(score:null,buroDisponible:true));
        Assert.False(fields["score_buro_disponible"].GetBoolean());
        Assert.Equal(-1,fields["score_buro"].GetInt32());
    }
    [Fact]
    public void ConditionOnScoreEvaluatesCorrectlyWhenAvailable()
    {
        var fields=PreevaluacionDecisions.Fields(Inputs(score:650));
        var condition=JsonSerializer.Serialize(new{operador="AND",condiciones=new object[]{
            new{campo="score_buro_disponible",operador="=",valor=true},
            new{campo="score_buro",operador="<=",valor=700}}});
        Assert.True(new RuleEngine().Evaluate(condition,fields));
    }
    [Fact]
    public void JudicialSeverityFieldsReflectGravedad()
    {
        var fields=PreevaluacionDecisions.Fields(Inputs(tieneProcesos:true,gravedad:"ALTA"));
        Assert.True(fields["tiene_procesos_judiciales"].GetBoolean());
        Assert.Equal("ALTA",fields["gravedad_judicial"].GetString());
    }
}
