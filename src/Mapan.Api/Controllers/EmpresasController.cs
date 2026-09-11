using Mapan.Application.Empresas;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController]
[Route("api/empresas")]
public sealed class EmpresasController(EmpresaService empresaService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EmpresaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmpresaDto>>> Get(CancellationToken cancellationToken)
    {
        var empresas = await empresaService.ObtenerEmpresasAsync(cancellationToken);
        return Ok(empresas);
    }
}
