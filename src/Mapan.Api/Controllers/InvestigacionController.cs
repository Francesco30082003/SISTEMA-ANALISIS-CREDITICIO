using Mapan.Application.Investigacion;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController,Route("api/solicitudes/{solicitudId:guid}/investigacion")]
public sealed class InvestigacionController(InvestigacionService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<InvestigacionResultadoDto>> GetUltima(Guid solicitudId,CancellationToken ct)
    {var r=await service.GetUltimaAsync(solicitudId,ct);return r is null?NoContent():Ok(r);}
    [HttpPost] public async Task<ActionResult<InvestigacionResultadoDto>> Ejecutar(Guid solicitudId,CancellationToken ct)=>Ok(await service.EjecutarAsync(solicitudId,ct));
}
