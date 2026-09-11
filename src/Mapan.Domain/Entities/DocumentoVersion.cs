namespace Mapan.Domain.Entities;

public sealed class DocumentoVersion
{
    public Guid DocumentoVersionId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid DocumentoId { get; set; }
    public int NumeroVersion { get; set; }
    public required string NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public long? TamanoBytes { get; set; }
    public required string AlmacenamientoUri { get; set; }
    public string? HashSha256 { get; set; }
    public required string Origen { get; set; }
    public Guid? CreadoPorUsuarioEmpresaId { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
