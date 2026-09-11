using Mapan.Application.Visitas;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController,Route("api/solicitudes/{solicitudId:guid}/visitas")]
public sealed class VisitasController(VisitaNegocioService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<VisitaNegocioDto>>> List(Guid solicitudId,CancellationToken ct)=>Ok(await service.ListAsync(solicitudId,ct));
    [HttpPost] public async Task<ActionResult<Guid>> Registrar(Guid solicitudId,VisitaNegocioInput input,CancellationToken ct)=>Ok(await service.RegistrarAsync(solicitudId,input,ct));
}
