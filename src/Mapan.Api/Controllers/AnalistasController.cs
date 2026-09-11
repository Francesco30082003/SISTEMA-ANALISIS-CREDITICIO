using Mapan.Application.Analistas;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController, Route("api/analistas")]
public sealed class AnalistasController(AnalistaDesempenoService service) : ControllerBase
{
    [HttpGet("desempeno")] public async Task<ActionResult<IReadOnlyList<AnalistaDesempenoDto>>> Desempeno(CancellationToken ct) => Ok(await service.ListAsync(ct));
}
