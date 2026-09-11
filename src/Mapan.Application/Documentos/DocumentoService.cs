using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Documentos;
public sealed record DocumentoDto(Guid SolicitudDocumentoId,Guid DocumentoVersionId,string Nombre,string TipoDocumento,int NumeroVersion,string Estado,string DocumentoEstado,string? MimeType);
public sealed record DocumentDownload(Stream Content,string MimeType,string FileName);
public sealed record DocumentReview(IReadOnlyList<Mapan.Domain.Entities.DatoExtraido> Extraidos,IReadOnlyList<Mapan.Domain.Entities.DatoValidado> Validaciones);
public sealed record ValidacionInput(Guid? DatoExtraidoId,[Required,MaxLength(120)]string CodigoCampo,
    [Required,RegularExpression("TEXTO|NUMERO|FECHA|BOOLEANO|JSON")]string TipoDato,
    string? ValorTexto,decimal? ValorNumerico,DateOnly? ValorFecha,bool? ValorBooleano,string? ValorJson,
    [Required,RegularExpression("CONFIRMADO|CORREGIDO|AGREGADO|DESCARTADO")]string Estado,string? Comentario);
public interface IDocumentoRepository
{
    Task<DocumentReview> ReviewAsync(Guid empresaId,Guid id,CancellationToken ct);
    Task<DocumentoDto> ReplaceAsync(Guid empresaId,Guid membershipId,Guid id,Stream content,string mime,CancellationToken ct);
    Task<IReadOnlyList<DocumentoDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct);
    Task<DocumentoDto> UploadAsync(Guid empresaId,Guid membershipId,Guid solicitudId,string nombre,string tipo,Stream content,string mime,CancellationToken ct);
    Task<DocumentDownload> DownloadAsync(Guid empresaId,Guid versionId,CancellationToken ct);
    Task<Guid> ValidateAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,ValidacionInput input,CancellationToken ct);
    Task VoidAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,CancellationToken ct);
}
public sealed class DocumentoService(IDocumentoRepository repository,ICurrentTenant tenant,IPermissionChecker permissions)
{
    public Task<DocumentReview> ReviewAsync(Guid id,CancellationToken ct){permissions.Require("Documentos:Read");return repository.ReviewAsync(tenant.EmpresaId,id,ct);}
    public Task<DocumentoDto> ReplaceAsync(Guid id,Stream content,string mime,CancellationToken ct){permissions.Require("Documentos:Upload");return repository.ReplaceAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,content,mime,ct);}
    public Task<IReadOnlyList<DocumentoDto>> ListAsync(Guid solicitudId,CancellationToken ct){permissions.Require("Documentos:Read");return repository.ListAsync(tenant.EmpresaId,solicitudId,ct);}
    public Task<DocumentoDto> UploadAsync(Guid solicitudId,string nombre,string tipo,Stream content,string mime,CancellationToken ct)
    {
        permissions.Require("Documentos:Upload");if(string.IsNullOrWhiteSpace(nombre)||nombre.Length>200||string.IsNullOrWhiteSpace(tipo)||tipo.Length>100)throw new ApplicationError(400,"VALIDATION","Nombre o tipo documental inválido.");
        return repository.UploadAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudId,nombre,tipo,content,mime,ct);
    }
    public Task<DocumentDownload> DownloadAsync(Guid versionId,CancellationToken ct){permissions.Require("Documentos:Read");return repository.DownloadAsync(tenant.EmpresaId,versionId,ct);}
    public Task<Guid> ValidateAsync(Guid id,ValidacionInput input,CancellationToken ct)
    {
        permissions.Require("Documentos:Validate");
        if(!Validator.TryValidateObject(input,new ValidationContext(input),[],true))throw new ApplicationError(400,"VALIDATION","Validación documental inválida.");
        var values=new Dictionary<string,bool>{{"TEXTO",input.ValorTexto is not null},{"NUMERO",input.ValorNumerico.HasValue},{"FECHA",input.ValorFecha.HasValue},{"BOOLEANO",input.ValorBooleano.HasValue},{"JSON",input.ValorJson is not null}};
        if(input.Estado!="DESCARTADO"&&(!values[input.TipoDato]||values.Count(v=>v.Value)!=1))throw new ApplicationError(400,"VALIDATION","Debe proporcionar un valor del tipo indicado.");
        if(input.ValorJson is not null){try{using var doc=System.Text.Json.JsonDocument.Parse(input.ValorJson);}catch(System.Text.Json.JsonException){throw new ApplicationError(400,"VALIDATION","JSON inválido.");}}
        return repository.ValidateAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,id,input,ct);
    }
    // El analista pudo equivocarse al cargar (documento incorrecto, tipo equivocado). Anular NUNCA borra
    // el archivo ni el registro — solo lo marca fuera de uso (Documento.Estado=ANULADO) para que quede
    // auditado y el expediente pueda cargarse de nuevo sin arrastrar el error.
    public Task VoidAsync(Guid solicitudDocumentoId,CancellationToken ct){permissions.Require("Documentos:Delete");return repository.VoidAsync(tenant.EmpresaId,tenant.UsuarioEmpresaId,solicitudDocumentoId,ct);}
}
