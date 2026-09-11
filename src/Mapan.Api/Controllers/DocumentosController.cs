using Mapan.Application.Documentos;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/documentos")]
public sealed class DocumentosController(DocumentoService service):ControllerBase
{
    [HttpGet("{id:guid}/revision")] public async Task<IActionResult> Review(Guid id,CancellationToken ct)=>Ok(await service.ReviewAsync(id,ct));
    [HttpPost("{id:guid}/versiones"),Consumes("multipart/form-data")]
    public async Task<IActionResult> Replace(Guid id,[FromForm]IFormFile archivo,CancellationToken ct)
    {await using var stream=archivo.OpenReadStream();return Ok(await service.ReplaceAsync(id,stream,archivo.ContentType,ct));}
    [HttpGet("solicitud/{solicitudId:guid}")]public async Task<ActionResult<IReadOnlyList<DocumentoDto>>> List(Guid solicitudId,CancellationToken ct)=>Ok(await service.ListAsync(solicitudId,ct));
    [HttpPost("solicitud/{solicitudId:guid}"),Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentoDto>> Upload(Guid solicitudId,[FromForm]string nombre,[FromForm]string tipo,[FromForm]IFormFile archivo,CancellationToken ct)
    {
        await using var stream=archivo.OpenReadStream();return Ok(await service.UploadAsync(solicitudId,nombre,tipo,stream,archivo.ContentType,ct));
    }
    [HttpGet("version/{versionId:guid}/archivo")]
    public async Task<IActionResult> Download(Guid versionId,CancellationToken ct)
    {
        var result=await service.DownloadAsync(versionId,ct);Response.Headers["X-Content-Type-Options"]="nosniff";return File(result.Content,result.MimeType,result.FileName);
    }
    [HttpPost("{solicitudDocumentoId:guid}/validaciones")]public async Task<ActionResult<Guid>> Validate(Guid solicitudDocumentoId,ValidacionInput input,CancellationToken ct)=>Ok(await service.ValidateAsync(solicitudDocumentoId,input,ct));
    [HttpPost("{solicitudDocumentoId:guid}/anular")]public async Task<IActionResult> Void(Guid solicitudDocumentoId,CancellationToken ct){await service.VoidAsync(solicitudDocumentoId,ct);return NoContent();}
}
