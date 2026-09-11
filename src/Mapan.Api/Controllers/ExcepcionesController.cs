using Mapan.Application.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

public sealed record ResolverExcepcionRequest(bool Aprobar,string? Comentario);

[ApiController,Route("api/excepciones")]
public sealed class ExcepcionesController(ExcepcionService service):ControllerBase
{
    [HttpGet("solicitud/{solicitudId:guid}")] public async Task<ActionResult<IReadOnlyList<ExcepcionDto>>> List(Guid solicitudId,CancellationToken ct)=>Ok(await service.ListAsync(solicitudId,ct));
    [HttpPost] public async Task<ActionResult<Guid>> Solicitar(ExcepcionInput input,CancellationToken ct)=>Ok(await service.SolicitarAsync(input,ct));
    [HttpPost("{id:guid}/resolver")] public async Task<IActionResult> Resolver(Guid id,ResolverExcepcionRequest input,CancellationToken ct)
    {await service.ResolverAsync(id,input.Aprobar,input.Comentario,ct);return NoContent();}
}
