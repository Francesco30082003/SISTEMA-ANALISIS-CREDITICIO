using Mapan.Application.Cartera;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController,Route("api/cosechas")]
public sealed class CosechasController(CosechaService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<CosechaDto>>> List(CancellationToken ct,Guid? productoCreditoId=null,Guid? sucursalId=null)=>Ok(await service.ListAsync(productoCreditoId,sucursalId,ct));
}
