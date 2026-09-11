using System.Text.Json;
using Mapan.Application.Operations;
using Microsoft.AspNetCore.Mvc;
namespace Mapan.Api.Controllers;
[ApiController,Route("api")]
public sealed class OperacionesController(OperationsService service):ControllerBase
{
    [HttpGet("auditoria")]public async Task<IActionResult> Audit([FromQuery]OperationQuery query,CancellationToken ct)=>Ok(await service.ReadAsync("auditoria",query,ct));
    [HttpGet("prestamos")]public async Task<IActionResult> Loans([FromQuery]OperationQuery query,CancellationToken ct)=>Ok(await service.ReadAsync("prestamos",query,ct));
    [HttpGet("prestamos/{id:guid}")]public async Task<IActionResult> Loan(Guid id,CancellationToken ct)=>Ok(await service.ReadAsync("prestamos",new(Id:id),ct));
    [HttpGet("prestamos/{id:guid}/desempeno")]public async Task<IActionResult> Cuts(Guid id,CancellationToken ct)=>Ok(await service.ReadAsync("desempeno",new(Id:id),ct));
    [HttpGet("workflow")]public async Task<IActionResult> Workflow(CancellationToken ct)=>Ok(await service.ReadAsync("workflow",new(),ct));
    [HttpGet("workflow/rutas")]public async Task<IActionResult> Routes(CancellationToken ct)=>Ok(await service.ReadAsync("rutas",new(),ct));
    [HttpGet("integraciones")]public async Task<IActionResult> Integrations(CancellationToken ct)=>Ok(await service.ReadAsync("integraciones",new(),ct));
    [HttpPost("prestamos")]public async Task<IActionResult> Loan(JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("prestamos",null,input,ct));
    [HttpPut("prestamos/{id:guid}")]public async Task<IActionResult> Loan(Guid id,JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("prestamos",id,input,ct));
    [HttpPost("desempeno")]public async Task<IActionResult> Cut(JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("desempeno",null,input,ct));
    [HttpPut("desempeno/{id:guid}")]public async Task<IActionResult> Cut(Guid id,JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("desempeno",id,input,ct));
    [HttpPost("workflow/rutas")]public async Task<IActionResult> Route(JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("rutas",null,input,ct));
    [HttpPut("workflow/rutas/{id:guid}")]public async Task<IActionResult> Route(Guid id,JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("rutas",id,input,ct));
    [HttpPost("workflow/pasos/{id:guid}/decisiones")]public async Task<IActionResult> Decision(Guid id,JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("decisiones",id,input,ct));
    [HttpPost("integraciones")]public async Task<IActionResult> Integration(JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("integraciones",null,input,ct));
    [HttpPut("integraciones/{id:guid}")]public async Task<IActionResult> Integration(Guid id,JsonElement input,CancellationToken ct)=>Ok(await service.WriteAsync("integraciones",id,input,ct));
}
