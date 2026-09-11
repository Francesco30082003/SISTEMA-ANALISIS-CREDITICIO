using Mapan.Application.Verificacion;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController,Route("api/solicitudes/{solicitudId:guid}/verificacion")]
public sealed class VerificacionController(VerificacionService service):ControllerBase
{
    [HttpGet("documentos")] public async Task<ActionResult<IReadOnlyList<DocumentoVerificacionDto>>> ListarDocumentos(Guid solicitudId,CancellationToken ct)=>Ok(await service.ListarDocumentosAsync(solicitudId,ct));
    [HttpPost("documentos/{solicitudDocumentoId:guid}")] public async Task<ActionResult<DocumentoVerificacionDto>> VerificarDocumento(Guid solicitudId,Guid solicitudDocumentoId,CancellationToken ct)=>Ok(await service.VerificarDocumentoAsync(solicitudDocumentoId,ct));
    [HttpGet("laboral")] public async Task<ActionResult<VerificacionLaboralDto>> GetUltimaLaboral(Guid solicitudId,CancellationToken ct)
    {var r=await service.GetUltimaLaboralAsync(solicitudId,ct);return r is null?NoContent():Ok(r);}
    [HttpPost("laboral")] public async Task<ActionResult<VerificacionLaboralDto>> EjecutarLaboral(Guid solicitudId,CancellationToken ct)=>Ok(await service.EjecutarLaboralAsync(solicitudId,ct));
}
