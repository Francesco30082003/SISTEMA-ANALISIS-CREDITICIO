using Mapan.Domain.Analysis;
using Mapan.Domain.Entities;

namespace Mapan.Domain.Tests;

public sealed class ObligacionCatalogTests
{
    private static Obligacion Debt(string? estado) => new(){ObligacionId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),SolicitudCreditoId=Guid.NewGuid(),Estado=estado,FechaCreacion=DateTimeOffset.UtcNow};
    [Fact] public void VigenteAndReestructuradaCountAsActive() => Assert.Equal(2, AnalysisDecisions.ActiveDebtCount([Debt("VIGENTE"), Debt("REESTRUCTURADA"), Debt("CANCELADA"), Debt("CASTIGADA")]));
    [Fact] public void UnknownOrNullEstadoDoesNotCountAsActive() => Assert.Equal(0, AnalysisDecisions.ActiveDebtCount([Debt(null), Debt("OTRO")]));
    [Fact] public void EmptyListHasNoActiveDebt() => Assert.Equal(0, AnalysisDecisions.ActiveDebtCount([]));
}
