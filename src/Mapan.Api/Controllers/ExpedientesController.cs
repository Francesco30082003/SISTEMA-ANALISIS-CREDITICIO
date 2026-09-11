using Mapan.Application.Expedientes;
using Microsoft.AspNetCore.Mvc;

namespace Mapan.Api.Controllers;
[ApiController,Route("api/solicitudes/{solicitudId:guid}/expediente")]
public sealed class ExpedientesController(ExpedienteService service):ControllerBase
{
    [HttpGet] public async Task<ActionResult<ExpedienteDto>> Get(Guid solicitudId,CancellationToken ct)=>Ok(await service.GetAsync(solicitudId,ct));
    [HttpPost("actividades")] public async Task<ActionResult<Guid>> Activity(Guid solicitudId,ActividadInput input,CancellationToken ct)=>Ok(await service.SaveActividadAsync(solicitudId,null,input,ct));
    [HttpPut("actividades/{id:guid}")] public async Task<ActionResult<Guid>> Activity(Guid solicitudId,Guid id,ActividadInput input,CancellationToken ct)=>Ok(await service.SaveActividadAsync(solicitudId,id,input,ct));
    [HttpPost("fuentes")] public async Task<ActionResult<Guid>> Source(Guid solicitudId,FuenteInput input,CancellationToken ct)=>Ok(await service.SaveFuenteAsync(solicitudId,null,input,ct));
    [HttpPut("fuentes/{id:guid}")] public async Task<ActionResult<Guid>> Source(Guid solicitudId,Guid id,FuenteInput input,CancellationToken ct)=>Ok(await service.SaveFuenteAsync(solicitudId,id,input,ct));
    [HttpPost("fuentes/{fuenteId:guid}/periodos")] public async Task<ActionResult<Guid>> Period(Guid solicitudId,Guid fuenteId,PeriodoInput input,CancellationToken ct)=>Ok(await service.SavePeriodoAsync(solicitudId,fuenteId,null,input,ct));
    [HttpPut("fuentes/{fuenteId:guid}/periodos/{id:guid}")] public async Task<ActionResult<Guid>> Period(Guid solicitudId,Guid fuenteId,Guid id,PeriodoInput input,CancellationToken ct)=>Ok(await service.SavePeriodoAsync(solicitudId,fuenteId,id,input,ct));
    [HttpPost("gastos")] public async Task<ActionResult<Guid>> Expense(Guid solicitudId,GastoInput input,CancellationToken ct)=>Ok(await service.SaveGastoAsync(solicitudId,null,input,ct));
    [HttpPut("gastos/{id:guid}")] public async Task<ActionResult<Guid>> Expense(Guid solicitudId,Guid id,GastoInput input,CancellationToken ct)=>Ok(await service.SaveGastoAsync(solicitudId,id,input,ct));
    [HttpPost("obligaciones")] public async Task<ActionResult<Guid>> Debt(Guid solicitudId,ObligacionInput input,CancellationToken ct)=>Ok(await service.SaveObligacionAsync(solicitudId,null,input,ct));
    [HttpPut("obligaciones/{id:guid}")] public async Task<ActionResult<Guid>> Debt(Guid solicitudId,Guid id,ObligacionInput input,CancellationToken ct)=>Ok(await service.SaveObligacionAsync(solicitudId,id,input,ct));
    [HttpDelete("fuentes/{fuenteId:guid}/periodos/{id:guid}")] public async Task<IActionResult> DeletePeriod(Guid solicitudId,Guid fuenteId,Guid id,CancellationToken ct){await service.DeletePeriodoAsync(solicitudId,fuenteId,id,ct);return NoContent();}
    [HttpDelete("gastos/{id:guid}")] public async Task<IActionResult> DeleteExpense(Guid solicitudId,Guid id,CancellationToken ct){await service.DeleteGastoAsync(solicitudId,id,ct);return NoContent();}
    [HttpDelete("obligaciones/{id:guid}")] public async Task<IActionResult> DeleteDebt(Guid solicitudId,Guid id,CancellationToken ct){await service.DeleteObligacionAsync(solicitudId,id,ct);return NoContent();}
}
