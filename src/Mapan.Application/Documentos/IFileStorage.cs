namespace Mapan.Application.Documentos;
public sealed record StoredFile(string Uri,string FileName,string MimeType,long Size,string Sha256);
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Guid empresaId,Stream source,string mimeType,CancellationToken ct);
    Task<Stream> OpenAsync(Guid empresaId,string uri,CancellationToken ct);
    Task RemoveUncommittedAsync(Guid empresaId,string uri,CancellationToken ct);
}
