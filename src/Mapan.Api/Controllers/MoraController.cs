using Mapan.Application.Cartera;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController, Route("api/mora")]
public sealed class MoraController(CarteraMoraService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<MoraClienteDto>>> List(CancellationToken ct) => Ok(await service.ListAsync(ct));
    [HttpPost("alertas")] public async Task<ActionResult<int>> EnviarAlerta(CancellationToken ct) => Ok(await service.EnviarAlertaAsync(ct));
}
