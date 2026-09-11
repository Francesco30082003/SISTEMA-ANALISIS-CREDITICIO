using Mapan.Application.Clientes;
using Mapan.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;

[ApiController, Route("api/clientes")]
public sealed class ClientesController(ClienteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<ClienteDto>>> List(CancellationToken ct, string? search=null, int page=1, int size=20) => Ok(await service.ListAsync(search,page,size,ct));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> Get(Guid id, CancellationToken ct) => Ok(await service.GetAsync(id,ct));
    [HttpPost]
    public async Task<ActionResult<ClienteDto>> Create(ClienteInput input, CancellationToken ct)
    {
        var result=await service.CreateAsync(input,ct); return CreatedAtAction(nameof(Get),new {id=result.ClienteId},result);
    }
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> Update(Guid id, ClienteInput input, CancellationToken ct) => Ok(await service.UpdateAsync(id,input,ct));
    [HttpPost("{id:guid}/inactivar")]
    public async Task<IActionResult> Inactivate(Guid id, CancellationToken ct) { await service.InactivateAsync(id,ct); return NoContent(); }
}
