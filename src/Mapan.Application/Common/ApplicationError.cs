namespace Mapan.Application.Common;

public sealed class ApplicationError(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public static ApplicationError Unauthorized() => new(401, "UNAUTHENTICATED", "Credenciales o sesión no válidas.");
    public static ApplicationError Forbidden() => new(403, "FORBIDDEN", "No tiene acceso a este recurso.");
    public static ApplicationError NotFound() => new(404, "NOT_FOUND", "Recurso no encontrado.");
    public static ApplicationError Configuration(string message) => new(422, "CONFIGURATION_REQUIRED", message);
}
