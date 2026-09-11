using System.Security.Cryptography;
using Mapan.Application.Common;
using Mapan.Application.Documentos;

namespace Mapan.Infrastructure.Storage;
public sealed record FileStorageSettings(string RootPath,long MaxBytes,IReadOnlySet<string> AllowedMimeTypes);
public sealed class LocalFileStorage(FileStorageSettings settings):IFileStorage
{
    public async Task<StoredFile> SaveAsync(Guid empresaId,Stream source,string mimeType,CancellationToken ct)
    {
        if(empresaId==Guid.Empty||!settings.AllowedMimeTypes.Contains(mimeType))throw Invalid("Tipo de archivo no permitido.");
        var header=new byte[8];var read=await source.ReadAtLeastAsync(header,8,false,ct);
        var extension=mimeType switch {
            "application/pdf" when read>=5&&header.AsSpan(0,5).SequenceEqual("%PDF-"u8)=>".pdf",
            "image/png" when read==8&&header.AsSpan().SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})=>".png",
            "image/jpeg" when read>=3&&header[0]==255&&header[1]==216&&header[2]==255=>".jpg",
            _=>throw Invalid("El contenido no coincide con un formato permitido.")
        };
        var name=Guid.NewGuid().ToString("N")+extension;var uri=empresaId.ToString("N")+"/"+name;
        var path=Resolve(empresaId,uri);Directory.CreateDirectory(Path.GetDirectoryName(path)!);CheckLinks(Path.GetDirectoryName(path)!);
        try {
            await using(var target=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true)) {
                using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long total=read;if(total>settings.MaxBytes)throw Invalid("Archivo demasiado grande.");
                hash.AppendData(header,0,read);await target.WriteAsync(header.AsMemory(0,read),ct);
                var buffer=new byte[81920];int length;
                while((length=await source.ReadAsync(buffer,ct))>0){total+=length;if(total>settings.MaxBytes)throw Invalid("Archivo demasiado grande.");hash.AppendData(buffer,0,length);await target.WriteAsync(buffer.AsMemory(0,length),ct);}
                await target.FlushAsync(ct);
                return new(uri,name,mimeType,total,Convert.ToHexStringLower(hash.GetHashAndReset()));
            }
        }catch {if(File.Exists(path))File.Delete(path);throw;}
    }
    public Task<Stream> OpenAsync(Guid empresaId,string uri,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();var path=Resolve(empresaId,uri);CheckLinks(path);
        if(!File.Exists(path))throw ApplicationError.NotFound();
        return Task.FromResult<Stream>(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true));
    }
    public Task RemoveUncommittedAsync(Guid empresaId,string uri,CancellationToken ct)
    {
        var path=Resolve(empresaId,uri);CheckLinks(path);if(File.Exists(path))File.Delete(path);return Task.CompletedTask;
    }
    private string Resolve(Guid empresaId,string uri)
    {
        var parts=uri.Split('/');
        if(parts.Length!=2||parts[0]!=empresaId.ToString("N")||!Guid.TryParseExact(Path.GetFileNameWithoutExtension(parts[1]),"N",out _)
            ||Path.GetExtension(parts[1]) is not (".pdf" or ".png" or ".jpg")||parts[1].IndexOfAny(['\\',':'])>=0)throw ApplicationError.Forbidden();
        var root=Path.GetFullPath(settings.RootPath);CheckLinks(root);
        return Path.Combine(root,parts[0],parts[1]);
    }
    private static void CheckLinks(string path)
    {
        for(var current=path;!string.IsNullOrEmpty(current);current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)
                throw ApplicationError.Forbidden();
    }
    private static ApplicationError Invalid(string message)=>new(400,"INVALID_FILE",message);
}
