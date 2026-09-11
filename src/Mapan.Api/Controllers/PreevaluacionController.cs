using Mapan.Application.Preevaluacion;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController,Route("api/solicitudes/{solicitudId:guid}/preevaluacion")]
public sealed class PreevaluacionController(PreevaluacionService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<PreevaluacionResumenDto>> GetUltima(Guid solicitudId,CancellationToken ct)
    {var r=await service.GetUltimaAsync(solicitudId,ct);return r is null?NoContent():Ok(r);}
    [HttpPost] public async Task<ActionResult<PreevaluacionResumenDto>> Ejecutar(Guid solicitudId,CancellationToken ct)=>Ok(await service.EjecutarAsync(solicitudId,ct));
}
