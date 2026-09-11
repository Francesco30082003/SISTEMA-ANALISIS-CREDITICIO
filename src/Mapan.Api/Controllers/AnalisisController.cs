using Mapan.Application.Analysis;
using Microsoft.AspNetCore.Mvc;
namespace Mapan.Api.Controllers;
[ApiController,Route("api/analisis")]
public sealed class AnalisisController(CreditAnalysisService service):ControllerBase
{
    [HttpGet]public async Task<IActionResult> List(CancellationToken ct,Guid? solicitudId=null)=>Ok(await service.ListAsync(solicitudId,ct));
    [HttpGet("{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(id,ct));
    [HttpPost("/api/solicitudes/{id:guid}/analisis")]public async Task<IActionResult> Execute(Guid id,CancellationToken ct)=>Ok(new{analisisId=await service.ExecuteAsync(id,ct)});
    [HttpGet("{id:guid}/informe")]public async Task<IActionResult> Report(Guid id,CancellationToken ct){
        var detail=await service.GetAsync(id,ct);
        Response.Headers.ContentDisposition=$"inline; filename=\"informe-ejecucion-{detail.Analisis.NumeroEjecucion}.pdf\"";
        return File(AnalysisReport.Render(detail),"application/pdf");
    }
    [HttpGet("/api/alertas")]public async Task<IActionResult> Alerts(CancellationToken ct)=>Ok(await service.AlertsAsync(ct));
    [HttpPost("/api/alertas/{id:guid}/resolver")]public async Task<IActionResult> Resolve(Guid id,CancellationToken ct){await service.ResolveAsync(id,ct);return NoContent();}
    [HttpPost("{id:guid}/decision")]public async Task<IActionResult> Decide(Guid id,DecisionInput input,CancellationToken ct)=>Ok(new{solicitudCreditoId=await service.DecideAsync(id,input.Decision,input.Comentario,ct)});
}
