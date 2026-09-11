using System.Security.Cryptography;
using Mapan.Application.Common;
using Mapan.Infrastructure.Storage;

namespace Mapan.Application.Tests;
public sealed class DocumentStorageTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"mapan-storage-test-"+Guid.NewGuid().ToString("N"));
    private LocalFileStorage Storage(long limit=1024)=>new(new(root,limit,new HashSet<string>{"application/pdf","image/png","image/jpeg"}));
    [Fact]
    public async Task ValidPdfRoundTripsWithVerifiedHashAndTenantBoundKey()
    {
        var tenant=Guid.NewGuid();var bytes="%PDF-1.7\nTest content"u8.ToArray();var storage=Storage();
        var file=await storage.SaveAsync(tenant,new MemoryStream(bytes),"application/pdf",default);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)),file.Sha256);
        Assert.Equal(bytes.Length,file.Size);
        await using var stream=await storage.OpenAsync(tenant,file.Uri,default);using var copy=new MemoryStream();await stream.CopyToAsync(copy);
        Assert.Equal(bytes,copy.ToArray());
        await Assert.ThrowsAsync<ApplicationError>(()=>storage.OpenAsync(Guid.NewGuid(),file.Uri,default));
    }
    [Theory]
    [InlineData("application/pdf","not a PDF")]
    [InlineData("image/png","%PDF-1.7")]
    [InlineData("text/html","<script>")]
    public async Task RejectsSpoofedOrDisallowedMime(string mime,string content)
    {
        var error=await Assert.ThrowsAsync<ApplicationError>(()=>Storage().SaveAsync(Guid.NewGuid(),new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),mime,default));
        Assert.Equal(400,error.Status);
    }
    [Fact]
    public async Task OversizeUploadLeavesNoPartialFile()
    {
        await Assert.ThrowsAsync<ApplicationError>(()=>Storage(8).SaveAsync(Guid.NewGuid(),new MemoryStream("%PDF-1.7\nToo large"u8.ToArray()),"application/pdf",default));
        Assert.Empty(Directory.GetFiles(root,"*",SearchOption.AllDirectories));
    }
    [Theory]
    [InlineData("../secret.pdf")]
    [InlineData("{tenant}/../../secret.pdf")]
    [InlineData("{tenant}/C:\\secret.pdf")]
    [InlineData("{tenant}/00000000000000000000000000000000.exe")]
    public async Task RejectsTraversalAndUnsafeKeys(string key)
    {
        var tenant=Guid.NewGuid();key=key.Replace("{tenant}",tenant.ToString("N"));
        await Assert.ThrowsAsync<ApplicationError>(()=>Storage().OpenAsync(tenant,key,default));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
