using Mapan.Application.Organizacion;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/organizacion")]
public sealed class OrganizacionController(OrganizacionService service):ControllerBase
{
    [HttpGet("sucursales")] public async Task<ActionResult<IReadOnlyList<SucursalDto>>> Branches(CancellationToken ct)=>Ok(await service.GetSucursalesAsync(ct));
    [HttpPut("sucursal-predeterminada/{sucursalId:guid}")] public async Task<IActionResult> DefaultBranch(Guid sucursalId,CancellationToken ct) {await service.SetDefaultBranchAsync(sucursalId,ct);return NoContent();}
}
