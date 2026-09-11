using Mapan.Application.Common;
using Mapan.Application.Solicitudes;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/solicitudes")]
public sealed class SolicitudesController(SolicitudService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<Page<SolicitudDto>>> List(CancellationToken ct,Guid? clienteId=null,int page=1,int size=20)=>Ok(await service.ListAsync(clienteId,page,size,ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<SolicitudDto>> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost] public async Task<ActionResult<SolicitudDto>> Create(SolicitudInput input,CancellationToken ct) {var result=await service.CreateAsync(input,ct);return CreatedAtAction(nameof(Get),new{id=result.SolicitudCreditoId},result);}
    [HttpPut("{id:guid}/borrador")] public async Task<ActionResult<SolicitudDto>> Update(Guid id,SolicitudInput input,CancellationToken ct)=>Ok(await service.UpdateDraftAsync(id,input,ct));
    [HttpPost("{id:guid}/enviar-documentacion")] public async Task<IActionResult> Send(Guid id,CancellationToken ct) {await service.SendToDocumentationAsync(id,ct);return NoContent();}
}
