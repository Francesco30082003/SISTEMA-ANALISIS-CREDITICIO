using Mapan.Domain.Credito;

namespace Mapan.Domain.Tests;

public sealed class CreditoFrancesCalculatorTests
{
    [Fact]
    public void MatchesTheReferenceWorkedExample()
    {
        // P=10000, tasa anual=16% (r=0.16/12=0.0133333), n=24 -> M=$489.63 (ejemplo de referencia del negocio).
        var r = CreditoFrancesCalculator.Calculate(10000m, 24, 16m);
        Assert.Equal(489.63m, r.CuotaEstimada, 2);
    }
    [Fact]
    public void FirstPeriodSplitAddsUpToTheInstallment()
    {
        var r = CreditoFrancesCalculator.Calculate(10000m, 24, 16m);
        Assert.Equal(r.CuotaEstimada, r.CapitalMensual + r.InteresMensual);
        Assert.Equal(Math.Round(10000m * 16m / 100m / 12m, 2), r.InteresMensual);
    }
    [Fact]
    public void TotalsAreDerivedFromTheFixedInstallment()
    {
        var r = CreditoFrancesCalculator.Calculate(10000m, 24, 16m);
        Assert.Equal(r.CuotaEstimada * 24, r.TotalAPagar);
        Assert.Equal(r.TotalAPagar - 10000m, r.InteresTotal);
    }
    [Fact]
    public void ZeroRateDegradesToFlatCapitalOnly()
    {
        var r = CreditoFrancesCalculator.Calculate(6000m, 6, 0m);
        Assert.Equal(1000m, r.CapitalMensual);
        Assert.Equal(0m, r.InteresMensual);
        Assert.Equal(1000m, r.CuotaEstimada);
        Assert.Equal(6000m, r.TotalAPagar);
    }
    [Fact]
    public void NullRateIsTreatedAsZero() =>
        Assert.Equal(CreditoFrancesCalculator.Calculate(6000m, 6, 0m), CreditoFrancesCalculator.Calculate(6000m, 6, null));
    [Theory]
    [InlineData(0, 12, 15)]
    [InlineData(1000, 0, 15)]
    [InlineData(-100, 12, 15)]
    public void InvalidMontoOrPlazoReturnsZeroSchedule(decimal monto, int plazo, decimal tasa)
    {
        var r = CreditoFrancesCalculator.Calculate(monto, plazo, tasa);
        Assert.Equal(0m, r.CapitalMensual);
        Assert.Equal(0m, r.InteresMensual);
        Assert.Equal(0m, r.CuotaEstimada);
        Assert.Equal(0m, r.InteresTotal);
        Assert.Equal(monto, r.TotalAPagar);
    }
}
