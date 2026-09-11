using System.Text.Json;
using Mapan.Domain.Analysis;

namespace Mapan.Domain.Tests;

public sealed class FinancialAndRuleTests
{
    private static FinancialInput Input(decimal? factor = 0.4m) => new([new([1500,1550,1550]),new([300])],[200],[1000],[100],200,factor);
    [Fact] public void AveragesEachSourceBeforeSumming() => Assert.Equal(1833.33m, decimal.Round(new CreditFinancialAnalysisEngine().Calculate(Input()).Income,2));
    [Fact] public void UsesPolicyFactor() { var result=new CreditFinancialAnalysisEngine().Calculate(Input(0.25m)); Assert.Equal(result.AvailableIncome*0.25m,result.NewInstallmentCapacity); }
    [Fact] public void MissingFactorFails() => Assert.Throws<AnalysisConfigurationException>(()=>new CreditFinancialAnalysisEngine().Calculate(Input(null)));
    [Fact] public void ZeroIncomeProducesNullRatios() { var result=new CreditFinancialAnalysisEngine().Calculate(new([],[],[],[],0,0.4m)); Assert.Null(result.CurrentDebtRatio); Assert.Null(result.PostDebtRatio); }
    [Fact] public void EmptySourceDoesNotInventIncome() => Assert.Throws<AnalysisConfigurationException>(()=>new CreditFinancialAnalysisEngine().Calculate(new([new([])],[],[],[],0,0.4m)));
    [Theory]
    [InlineData(">",10,9,true)] [InlineData(">",9,9,false)] [InlineData(">=",9,9,true)]
    [InlineData("<",8,9,true)] [InlineData("<=",9,9,true)] [InlineData("=",9,9,true)] [InlineData("!=",8,9,true)]
    public void Comparisons(string op,int a,int b,bool expected) => Assert.Equal(expected,new RuleEngine().Evaluate(
        JsonSerializer.Serialize(new {campo="a",operador=op,comparar_con="b"}),Fields(a,b)));
    [Theory] [InlineData("AND",false)] [InlineData("OR",true)]
    public void LogicalOperators(string op,bool expected) => Assert.Equal(expected,new RuleEngine().Evaluate(
        JsonSerializer.Serialize(new {operador=op,condiciones=new[]{new {campo="a",operador=">",valor=5},new {campo="b",operador=">",valor=5}}}),Fields(10,1)));
    [Fact] public void UnknownOperatorFailsEvenInUnusedBranch() => Assert.Throws<AnalysisConfigurationException>(()=>new RuleEngine().Evaluate(
        """{"operador":"OR","condiciones":[{"campo":"a","operador":">","valor":1},{"campo":"a","operador":"EXEC","valor":1}]}""",Fields(10,1)));
    [Fact] public void NullIsNotConvertedToZero() => Assert.Throws<AnalysisConfigurationException>(()=>new RuleEngine().Evaluate(
        """{"campo":"a","operador":"=","valor":0}""",new Dictionary<string,JsonElement>{{"a",JsonSerializer.SerializeToElement<object?>(null)}}));
    [Fact] public void UnknownActionFails() => Assert.Throws<AnalysisConfigurationException>(()=>new RuleEngine().ParseAction("""{"accion":"EXEC","resultado":"X"}"""));
    private static Dictionary<string,JsonElement> Fields(int a,int b)=>new(){{"a",JsonSerializer.SerializeToElement(a)},{"b",JsonSerializer.SerializeToElement(b)}};
}
