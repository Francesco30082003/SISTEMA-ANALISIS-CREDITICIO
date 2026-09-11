using Mapan.Application.Productos;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/productos")]
public sealed class ProductosController(ProductoService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ProductoDto>>> List(CancellationToken ct,bool soloActivos=true)=>Ok(await service.ListAsync(soloActivos,ct));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ProductoDto>> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost] public async Task<ActionResult<ProductoDto>> Create(ProductoInput input,CancellationToken ct) {var p=await service.CreateAsync(input,ct);return CreatedAtAction(nameof(Get),new{id=p.ProductoCreditoId},p);}
    [HttpPut("{id:guid}")] public async Task<ActionResult<ProductoDto>> Update(Guid id,ProductoInput input,CancellationToken ct)=>Ok(await service.UpdateAsync(id,input,ct));
    [HttpPost("{id:guid}/simular")] public async Task<ActionResult<Mapan.Domain.Credito.CreditoCalculoResultado>> Simular(Guid id,SimularInput input,CancellationToken ct)=>Ok(await service.SimularAsync(id,input,ct));
    [HttpPost("{id:guid}/activar")] public async Task<IActionResult> Activate(Guid id,CancellationToken ct) {await service.SetActiveAsync(id,true,ct);return NoContent();}
    [HttpPost("{id:guid}/inactivar")] public async Task<IActionResult> Inactivate(Guid id,CancellationToken ct) {await service.SetActiveAsync(id,false,ct);return NoContent();}
}
