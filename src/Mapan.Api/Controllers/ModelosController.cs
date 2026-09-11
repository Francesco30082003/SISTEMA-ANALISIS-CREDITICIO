using Mapan.Application.Modelos;
using Microsoft.AspNetCore.Mvc;
namespace Mapan.Api.Controllers;
[ApiController,Route("api/modelos")]
public sealed class ModelosController(ModeloService service):ControllerBase
{
    [HttpGet]public async Task<IActionResult> List(CancellationToken ct)=>Ok(await service.ListAsync(ct));
    [HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost]public async Task<IActionResult> Create(ModeloInput input,CancellationToken ct)=>Ok(await service.SaveAsync(null,input,ct));
    [HttpPut("{id:guid}")]public async Task<IActionResult> Update(Guid id,ModeloInput input,CancellationToken ct)=>Ok(await service.SaveAsync(id,input,ct));
    [HttpPost("{id:guid}/estado")]public async Task<IActionResult> State(Guid id,[FromBody]string state,CancellationToken ct){await service.StateAsync(id,state,ct);return NoContent();}
    [HttpPost("{id:guid}/versiones")]public async Task<IActionResult> CreateVersion(Guid id,ModeloVersionInput input,CancellationToken ct)=>Ok(await service.CreateVersionAsync(id,input,ct));
    [HttpPut("versiones/{id:guid}")]public async Task<IActionResult> UpdateVersion(Guid id,ModeloVersionInput input,CancellationToken ct){await service.SaveVersionAsync(id,input,ct);return NoContent();}
    [HttpPost("versiones/{id:guid}/estado")]public async Task<IActionResult> VersionState(Guid id,[FromBody]string state,CancellationToken ct){await service.VersionStateAsync(id,state,ct);return NoContent();}
}
