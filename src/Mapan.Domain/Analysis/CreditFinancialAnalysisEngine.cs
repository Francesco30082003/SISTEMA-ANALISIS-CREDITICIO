namespace Mapan.Domain.Analysis;

public sealed class AnalysisConfigurationException(string message) : Exception(message);
public sealed record IncomeSource(IReadOnlyList<decimal> Periods);
public sealed record FinancialInput(IReadOnlyList<IncomeSource> Sources, IReadOnlyList<decimal> MonthlyExpenses,
    IReadOnlyList<decimal> DebtBalances, IReadOnlyList<decimal> CurrentInstallments, decimal NewInstallment, decimal? CapacityFactor);
public sealed record FinancialResult(decimal Income, decimal Expenses, decimal AvailableIncome, decimal CapacityFactor,
    decimal NewInstallmentCapacity, decimal Debt, decimal CurrentInstallments, decimal NewInstallment,
    decimal? CurrentDebtRatio, decimal? PostDebtRatio, bool Compatible);

public sealed class CreditFinancialAnalysisEngine
{
    public FinancialResult Calculate(FinancialInput input)
    {
        if (input.CapacityFactor is null) throw new AnalysisConfigurationException("Falta FACTOR_CAPACIDAD de la versión de política utilizada.");
        if (input.CapacityFactor is < 0 or > 1) throw new AnalysisConfigurationException("FACTOR_CAPACIDAD debe estar entre 0 y 1.");
        if (input.Sources.Any(s => s.Periods.Count == 0)) throw new AnalysisConfigurationException("Una fuente de ingreso no tiene períodos confirmados.");
        if (input.Sources.SelectMany(s => s.Periods).Concat(input.MonthlyExpenses).Concat(input.DebtBalances)
            .Concat(input.CurrentInstallments).Append(input.NewInstallment).Any(v => v < 0))
            throw new ArgumentException("Los importes de entrada no pueden ser negativos.");
        var income = input.Sources.Sum(source => source.Periods.Average());
        var expenses = input.MonthlyExpenses.Sum();
        var available = income - expenses;
        var capacity = available * input.CapacityFactor.Value;
        var installments = input.CurrentInstallments.Sum();
        return new(income, expenses, available, input.CapacityFactor.Value, capacity, input.DebtBalances.Sum(),
            installments, input.NewInstallment, Ratio(installments, income), Ratio(installments + input.NewInstallment, income),
            input.NewInstallment <= capacity);
    }
    public static decimal? Ratio(decimal numerator, decimal denominator) => denominator == 0 ? null : numerator / denominator;
}
