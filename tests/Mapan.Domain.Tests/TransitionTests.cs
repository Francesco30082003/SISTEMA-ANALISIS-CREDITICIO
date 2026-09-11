using Mapan.Domain.Credito;
namespace Mapan.Domain.Tests;
public sealed class TransitionTests
{
    [Fact] public void DraftCanBeSent()=>Assert.Equal("DOCUMENTACION",SolicitudTransitions.SendToDocumentation("BORRADOR"));
    [Theory] [InlineData("APROBADA")] [InlineData("CANCELADA")] [InlineData("DOCUMENTACION")]
    public void OtherStatesCannotBeSent(string state)=>Assert.Throws<InvalidOperationException>(()=>SolicitudTransitions.SendToDocumentation(state));
    [Fact] public void HistoricalApplicationCannotBeEdited()=>Assert.Throws<InvalidOperationException>(()=>SolicitudTransitions.RequireDraft("APROBADA"));
}
