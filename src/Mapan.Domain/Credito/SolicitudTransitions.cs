namespace Mapan.Domain.Credito;

public static class SolicitudTransitions
{
    public static string SendToDocumentation(string current) => current == "BORRADOR" ? "DOCUMENTACION"
        : throw new InvalidOperationException("Solo un borrador puede enviarse a documentación.");
    public static void RequireDraft(string current)
    {
        if (current != "BORRADOR") throw new InvalidOperationException("Solo se puede editar una solicitud en borrador.");
    }
}
