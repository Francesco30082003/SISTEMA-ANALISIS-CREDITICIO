using Mapan.Application.Common;
using Mapan.Application.Expedientes;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class ExpedienteRepository(MapanDbContext db,AuditWriter audit):IExpedienteRepository
{
    public async Task<ExpedienteDto> GetAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct))throw ApplicationError.NotFound();
        var activities=await db.Set<ActividadEconomica>().AsNoTracking().Where(a=>a.EmpresaId==empresaId&&a.SolicitudCreditoId==solicitudId).OrderBy(a=>a.FechaCreacion).ToListAsync(ct);
        var sources=await db.Set<FuenteIngreso>().AsNoTracking().Where(f=>f.EmpresaId==empresaId&&f.SolicitudCreditoId==solicitudId).OrderBy(f=>f.FechaCreacion).ToListAsync(ct);
        var ids=sources.Select(f=>f.FuenteIngresoId).ToArray();
        var periods=await db.Set<IngresoPeriodo>().AsNoTracking().Where(p=>p.EmpresaId==empresaId&&ids.Contains(p.FuenteIngresoId)).OrderBy(p=>p.PeriodoInicio).ToListAsync(ct);
        var expenses=await db.Set<Gasto>().AsNoTracking().Where(g=>g.EmpresaId==empresaId&&g.SolicitudCreditoId==solicitudId).OrderBy(g=>g.FechaCreacion).ToListAsync(ct);
        var debts=await db.Set<Obligacion>().AsNoTracking().Where(o=>o.EmpresaId==empresaId&&o.SolicitudCreditoId==solicitudId).OrderBy(o=>o.FechaCreacion).ToListAsync(ct);
        return new(activities,sources,periods,expenses,debts);
    }
    public async Task<Guid> SaveActividadAsync(Guid empresaId,Guid solicitudId,Guid? id,ActividadInput input,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var a=id.HasValue?await db.Set<ActividadEconomica>().SingleOrDefaultAsync(a=>a.EmpresaId==empresaId&&a.SolicitudCreditoId==solicitudId&&a.ActividadEconomicaId==id,ct)??throw ApplicationError.NotFound()
            :new ActividadEconomica {ActividadEconomicaId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,TipoActividad=input.TipoActividad};
        a.TipoActividad=input.TipoActividad;a.EmpleadorNegocio=input.EmpleadorNegocio;a.CargoActividad=input.CargoActividad;a.FechaInicio=input.FechaInicio;a.Ruc=input.Ruc;a.Descripcion=input.Descripcion;a.EsPrincipal=input.EsPrincipal;
        if(!id.HasValue)db.Add(a);audit.Add("credito.actividad_economica",a.ActividadEconomicaId,"GUARDAR");await FinishAsync(tx,ct);return a.ActividadEconomicaId;
    }
    public async Task<Guid> SaveFuenteAsync(Guid empresaId,Guid solicitudId,Guid? id,FuenteInput input,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        if(input.ActividadEconomicaId.HasValue&&!await db.Set<ActividadEconomica>().AnyAsync(a=>a.EmpresaId==empresaId&&a.SolicitudCreditoId==solicitudId&&a.ActividadEconomicaId==input.ActividadEconomicaId,ct))throw ApplicationError.NotFound();
        var f=id.HasValue?await db.Set<FuenteIngreso>().SingleOrDefaultAsync(f=>f.EmpresaId==empresaId&&f.SolicitudCreditoId==solicitudId&&f.FuenteIngresoId==id,ct)??throw ApplicationError.NotFound()
            :new FuenteIngreso {FuenteIngresoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,TipoIngreso=input.TipoIngreso,MonedaCodigo=input.MonedaCodigo,Declarado=true};
        f.ActividadEconomicaId=input.ActividadEconomicaId;f.TipoIngreso=input.TipoIngreso;f.Descripcion=input.Descripcion;f.MonedaCodigo=input.MonedaCodigo;f.EsRecurrente=input.EsRecurrente;
        if(!id.HasValue)db.Add(f);audit.Add("credito.fuente_ingreso",f.FuenteIngresoId,"GUARDAR");await FinishAsync(tx,ct);return f.FuenteIngresoId;
    }
    public async Task<Guid> SavePeriodoAsync(Guid empresaId,Guid solicitudId,Guid fuenteId,Guid? id,PeriodoInput input,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        if(!await db.Set<FuenteIngreso>().AnyAsync(f=>f.EmpresaId==empresaId&&f.SolicitudCreditoId==solicitudId&&f.FuenteIngresoId==fuenteId,ct))throw ApplicationError.NotFound();
        var p=id.HasValue?await db.Set<IngresoPeriodo>().SingleOrDefaultAsync(p=>p.EmpresaId==empresaId&&p.FuenteIngresoId==fuenteId&&p.IngresoPeriodoId==id,ct)??throw ApplicationError.NotFound()
            :new IngresoPeriodo {IngresoPeriodoId=Guid.NewGuid(),EmpresaId=empresaId,FuenteIngresoId=fuenteId};
        p.PeriodoInicio=input.PeriodoInicio;p.PeriodoFin=input.PeriodoFin;p.MontoBruto=input.MontoBruto;p.MontoNeto=input.MontoNeto;p.Observacion=input.Observacion;
        if(!id.HasValue)db.Add(p);audit.Add("credito.ingreso_periodo",p.IngresoPeriodoId,"GUARDAR");await FinishAsync(tx,ct);return p.IngresoPeriodoId;
    }
    public async Task<Guid> SaveGastoAsync(Guid empresaId,Guid solicitudId,Guid? id,GastoInput input,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var g=id.HasValue?await db.Set<Gasto>().SingleOrDefaultAsync(g=>g.EmpresaId==empresaId&&g.SolicitudCreditoId==solicitudId&&g.GastoId==id,ct)??throw ApplicationError.NotFound()
            :new Gasto {GastoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,TipoGasto=input.TipoGasto,Declarado=true};
        g.TipoGasto=input.TipoGasto;g.Descripcion=input.Descripcion;g.MontoMensual=input.MontoMensual;
        if(!id.HasValue)db.Add(g);audit.Add("credito.gasto",g.GastoId,"GUARDAR");await FinishAsync(tx,ct);return g.GastoId;
    }
    public async Task<Guid> SaveObligacionAsync(Guid empresaId,Guid solicitudId,Guid? id,ObligacionInput input,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var o=id.HasValue?await db.Set<Obligacion>().SingleOrDefaultAsync(o=>o.EmpresaId==empresaId&&o.SolicitudCreditoId==solicitudId&&o.ObligacionId==id,ct)??throw ApplicationError.NotFound()
            :new Obligacion {ObligacionId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId};
        o.Institucion=input.Institucion;o.TipoObligacion=input.TipoObligacion;o.NumeroOperacionMascara=input.NumeroOperacionMascara;o.MontoOriginal=input.MontoOriginal;o.SaldoActual=input.SaldoActual;o.CuotaMensual=input.CuotaMensual;o.DiasMoraActual=input.DiasMoraActual;o.MaxDiasMoraHistorico=input.MaxDiasMoraHistorico;o.Estado=input.Estado;o.EsGarante=input.EsGarante;
        if(!id.HasValue)db.Add(o);audit.Add("credito.obligacion",o.ObligacionId,"GUARDAR");await FinishAsync(tx,ct);return o.ObligacionId;
    }
    public async Task DeletePeriodoAsync(Guid empresaId,Guid solicitudId,Guid fuenteId,Guid id,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var p=await db.Set<IngresoPeriodo>().SingleOrDefaultAsync(p=>p.EmpresaId==empresaId&&p.FuenteIngresoId==fuenteId&&p.IngresoPeriodoId==id,ct)??throw ApplicationError.NotFound();
        db.Remove(p);audit.Add("credito.ingreso_periodo",id,"ELIMINAR");await FinishAsync(tx,ct);
    }
    public async Task DeleteGastoAsync(Guid empresaId,Guid solicitudId,Guid id,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var g=await db.Set<Gasto>().SingleOrDefaultAsync(g=>g.EmpresaId==empresaId&&g.SolicitudCreditoId==solicitudId&&g.GastoId==id,ct)??throw ApplicationError.NotFound();
        db.Remove(g);audit.Add("credito.gasto",id,"ELIMINAR");await FinishAsync(tx,ct);
    }
    public async Task DeleteObligacionAsync(Guid empresaId,Guid solicitudId,Guid id,CancellationToken ct)
    {
        await using var tx=await BeginAsync(empresaId,solicitudId,ct);
        var o=await db.Set<Obligacion>().SingleOrDefaultAsync(o=>o.EmpresaId==empresaId&&o.SolicitudCreditoId==solicitudId&&o.ObligacionId==id,ct)??throw ApplicationError.NotFound();
        db.Remove(o);audit.Add("credito.obligacion",id,"ELIMINAR");await FinishAsync(tx,ct);
    }
    private async Task<IDbContextTransaction> BeginAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        var tx=await db.Database.BeginTransactionAsync(ct);
        try {
            var parent=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={empresaId} AND solicitud_credito_id={solicitudId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
            if(parent.Estado!="BORRADOR")throw new ApplicationError(422,"INVALID_TRANSITION","La captura de datos requiere una solicitud en borrador.");
            return tx;
        }catch {await tx.DisposeAsync();throw;}
    }
    private async Task FinishAsync(IDbContextTransaction tx,CancellationToken ct) {await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}
}
