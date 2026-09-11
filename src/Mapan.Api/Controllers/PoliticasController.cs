using Mapan.Application.Policies;
using Microsoft.AspNetCore.Mvc;
namespace Mapan.Api.Controllers;
[ApiController,Route("api/politicas")]
public sealed class PoliticasController(PolicyService service):ControllerBase
{
    [HttpGet]public async Task<IActionResult> List(CancellationToken ct)=>Ok(await service.ListAsync(ct));
    [HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost]public async Task<IActionResult> Create(PolicyInput input,CancellationToken ct)=>Ok(await service.SaveAsync(null,input,ct));
    [HttpPut("{id:guid}")]public async Task<IActionResult> Update(Guid id,PolicyInput input,CancellationToken ct)=>Ok(await service.SaveAsync(id,input,ct));
    [HttpPost("{id:guid}/versiones")]public async Task<IActionResult> CreateVersion(Guid id,VersionInput input,CancellationToken ct)=>Ok(await service.CreateVersionAsync(id,input,ct));
    [HttpGet("versiones/{id:guid}")]public async Task<IActionResult> Version(Guid id,CancellationToken ct)=>Ok(await service.VersionAsync(id,ct));
    [HttpPost("versiones/{id:guid}/estado")]public async Task<IActionResult> State(Guid id,[FromBody]string state,CancellationToken ct){await service.StateAsync(id,state,ct);return NoContent();}
    [HttpPost("versiones/{version:guid}/parametros")]public async Task<IActionResult> Parameter(Guid version,ParameterInput input,CancellationToken ct){await service.ParameterAsync(version,null,input,ct);return NoContent();}
    [HttpPut("versiones/{version:guid}/parametros/{id:guid}")]public async Task<IActionResult> Parameter(Guid version,Guid id,ParameterInput input,CancellationToken ct){await service.ParameterAsync(version,id,input,ct);return NoContent();}
    [HttpPost("versiones/{version:guid}/reglas")]public async Task<IActionResult> Rule(Guid version,RuleInput input,CancellationToken ct){await service.RuleAsync(version,null,input,ct);return NoContent();}
    [HttpPut("versiones/{version:guid}/reglas/{id:guid}")]public async Task<IActionResult> Rule(Guid version,Guid id,RuleInput input,CancellationToken ct){await service.RuleAsync(version,id,input,ct);return NoContent();}
    [HttpDelete("versiones/{version:guid}/parametros/{id:guid}")]public async Task<IActionResult> DeleteParameter(Guid version,Guid id,CancellationToken ct){await service.DeleteAsync(version,id,false,ct);return NoContent();}
    [HttpDelete("versiones/{version:guid}/reglas/{id:guid}")]public async Task<IActionResult> DeleteRule(Guid version,Guid id,CancellationToken ct){await service.DeleteAsync(version,id,true,ct);return NoContent();}
}
