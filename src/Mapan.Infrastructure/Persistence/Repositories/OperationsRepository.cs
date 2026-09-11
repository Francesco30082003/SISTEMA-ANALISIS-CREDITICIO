using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Operations;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class OperationsRepository(MapanDbContext db,AuditWriter audit,IEmailSender email):IOperationsRepository
{
    public async Task<object> ReadAsync(string area,Guid tenant,Guid member,OperationQuery q,CancellationToken ct)
    {
        switch(area){
        case "auditoria":
            var events=db.Set<Evento>().AsNoTracking().Where(x=>x.EmpresaId==tenant);
            if(q.Desde.HasValue)events=events.Where(x=>x.FechaEvento>=q.Desde.Value.ToUniversalTime());if(q.Hasta.HasValue)events=events.Where(x=>x.FechaEvento<=q.Hasta.Value.ToUniversalTime());
            if(q.Usuario.HasValue)events=events.Where(x=>x.UsuarioEmpresaId==q.Usuario);if(!string.IsNullOrWhiteSpace(q.Entidad))events=events.Where(x=>x.EntidadTipo==q.Entidad);if(!string.IsNullOrWhiteSpace(q.Accion))events=events.Where(x=>x.Accion==q.Accion);if(!string.IsNullOrWhiteSpace(q.CorrelationId))events=events.Where(x=>x.CorrelationId==q.CorrelationId);
            return new{total=await events.CountAsync(ct),items=await events.OrderByDescending(x=>x.FechaEvento).ThenBy(x=>x.EventoId).Skip((q.Page-1)*q.Size).Take(q.Size).Select(x=>new{x.FechaEvento,x.EntidadTipo,x.Accion,x.Exitoso,x.CorrelationId,Usuario=db.Set<Usuario>().Where(u=>u.UsuarioId==x.UsuarioId).Select(u=>u.NombreUsuario).FirstOrDefault()}).ToListAsync(ct)};
        case "prestamos":
            // Mismo criterio de alcance que CarteraMoraRepository/AnalistaDesempenoRepository: un jefe
            // de agencia (sucursal_predeterminada_id no nula) solo ve los préstamos de SU sucursal;
            // gerencia/auditoría/admin (sin sucursal fija) ven toda la empresa.
            var sucursalUsuarioPrestamos=await db.Set<UsuarioEmpresa>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.UsuarioEmpresaId==member).Select(x=>x.SucursalPredeterminadaId).SingleOrDefaultAsync(ct);
            return await(from p in db.Set<Prestamo>().AsNoTracking() join s in db.Set<SolicitudCredito>() on p.SolicitudCreditoId equals s.SolicitudCreditoId join suc in db.Set<Sucursal>() on s.SucursalId equals suc.SucursalId where p.EmpresaId==tenant&&s.EmpresaId==tenant&&suc.EmpresaId==tenant&&(q.Id==null||p.PrestamoId==q.Id)&&(sucursalUsuarioPrestamos==null||s.SucursalId==sucursalUsuarioPrestamos) orderby p.FechaCreacion descending select new{p.PrestamoId,p.SolicitudCreditoId,p.NumeroPrestamo,p.MontoDesembolsado,p.FechaDesembolso,p.PlazoMeses,p.TasaInteresAnualPct,p.CuotaPactada,p.FechaVencimiento,p.Estado,Solicitud=s.NumeroSolicitud,SucursalId=suc.SucursalId,Sucursal=suc.Nombre}).Skip((q.Page-1)*q.Size).Take(q.Size).ToListAsync(ct);
        case "desempeno":
            if(!await db.Set<Prestamo>().AnyAsync(x=>x.EmpresaId==tenant&&x.PrestamoId==q.Id,ct))throw ApplicationError.NotFound();
            return await db.Set<DesempenoCredito>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.PrestamoId==q.Id).OrderByDescending(x=>x.FechaCorte).ToListAsync(ct);
        case "workflow":return await new WorkflowRepository(db,audit,email).InboxAsync(tenant,member,ct);
        case "rutas":
            var routes=await db.Set<RutaAprobacion>().AsNoTracking().Where(x=>x.EmpresaId==tenant).OrderBy(x=>x.Prioridad).ToListAsync(ct);
            var steps=await(from s in db.Set<RutaAprobacionPaso>().AsNoTracking() join r in db.Set<Rol>() on s.RolId equals r.RolId where s.EmpresaId==tenant&&r.EmpresaId==tenant orderby s.Orden select new{s.RutaAprobacionPasoId,s.RutaAprobacionId,s.Orden,s.Nombre,s.RolId,Rol=r.Nombre,s.CantidadAprobacionesRequeridas,s.PermiteAprobar,s.PermiteRechazar,s.PermiteDevolver}).ToListAsync(ct);
            return new{rutas=routes,pasos=steps,roles=await db.Set<Rol>().AsNoTracking().Where(x=>x.EmpresaId==tenant&&x.Estado=="ACTIVO").Select(x=>new{x.RolId,x.Nombre}).ToListAsync(ct)};
        case "integraciones":return new{proveedores=await db.Set<Proveedor>().AsNoTracking().Where(x=>x.Activo).Select(x=>new{x.ProveedorId,x.Nombre,x.Tipo}).ToListAsync(ct),configuraciones=await(from e in db.Set<EmpresaProveedor>().AsNoTracking() join p in db.Set<Proveedor>() on e.ProveedorId equals p.ProveedorId where e.EmpresaId==tenant select new{e.EmpresaProveedorId,e.ProveedorId,p.Nombre,p.Tipo,e.Ambiente,e.Activo,Estado="NO_CONFIGURADO"}).ToListAsync(ct)};
        default:throw ApplicationError.NotFound();
        }
    }
    private static string Text(JsonElement input,string key,int max=200,bool required=true){var value=input.TryGetProperty(key,out var p)&&p.ValueKind==JsonValueKind.String?p.GetString():null;if((required&&string.IsNullOrWhiteSpace(value))||value?.Length>max)throw Invalid("Revisa el campo "+key+".");return value??"";}
    private static Guid Id(JsonElement input,string key){if(!input.TryGetProperty(key,out var value)||!value.TryGetGuid(out var id)||id==Guid.Empty)throw Invalid("Selecciona "+key+".");return id;}
    private static decimal Number(JsonElement input,string key){if(!input.TryGetProperty(key,out var value)||!value.TryGetDecimal(out var number)||number<0)throw Invalid("Confirma un valor no negativo para "+key+".");return number;}
    private static int Integer(JsonElement input,string key){var n=Number(input,key);if(n!=decimal.Truncate(n)||n>int.MaxValue)throw Invalid("Se requiere un número entero en "+key+".");return (int)n;}
    private static bool Flag(JsonElement input,string key)=>input.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.True;
    private static DateOnly Date(JsonElement input,string key){if(!DateOnly.TryParse(Text(input,key),out var date))throw Invalid("Revisa la fecha "+key+".");return date;}
    public async Task<Guid> WriteAsync(string area,Guid tenant,Guid member,Guid? id,JsonElement input,CancellationToken ct)
    {
        if(area=="decisiones"){
            var workflow=new WorkflowRepository(db,audit,email);
            var(decisionId,siguiente)=await workflow.DecideAsync(tenant,member,id??throw ApplicationError.NotFound(),Text(input,"decision",30),Text(input,"comentario",4000,false),ct);
            await workflow.NotifyAsync(tenant,siguiente,ct);
            return decisionId;
        }
        await using var tx=await db.Database.BeginTransactionAsync(ct);Guid saved;
        switch(area){
        case "prestamos":
            var requestId=Id(input,"solicitudCreditoId");
            var s=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={tenant} AND solicitud_credito_id={requestId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
            if(!Flag(input,"confirmacionHumana"))throw Invalid("Confirma expresamente el registro autorizado del préstamo.");
            if(s.Estado is "RECHAZADA" or "CANCELADA")throw Invalid("La solicitud está rechazada o cancelada.");
            if(await(from a in db.Set<Aprobacion>() join r in db.Set<Recomendacion>() on a.RecomendacionId equals r.RecomendacionId join an in db.Set<Analisis>() on r.AnalisisId equals an.AnalisisId where a.EmpresaId==tenant&&r.EmpresaId==tenant&&an.EmpresaId==tenant&&an.SolicitudCreditoId==requestId&&(a.Estado=="EN_CURSO"||a.Estado=="PENDIENTE") select a).AnyAsync(ct))throw Invalid("Completa la aprobación pendiente antes de registrar el préstamo.");
            var loan=id.HasValue?await db.Set<Prestamo>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PrestamoId==id,ct)??throw ApplicationError.NotFound():new Prestamo{PrestamoId=Guid.NewGuid(),EmpresaId=tenant,SolicitudCreditoId=requestId,NumeroPrestamo=Text(input,"numeroPrestamo",80),Estado="VIGENTE"};
            if(loan.SolicitudCreditoId!=requestId)throw Invalid("No se puede cambiar la solicitud de un préstamo.");
            loan.NumeroPrestamo=Text(input,"numeroPrestamo",80);loan.MontoDesembolsado=Number(input,"montoDesembolsado");loan.PlazoMeses=Integer(input,"plazoMeses");loan.FechaDesembolso=Date(input,"fechaDesembolso");loan.CuotaPactada=Number(input,"cuotaPactada");loan.Estado=Text(input,"estado",30);
            if(loan.MontoDesembolsado<=0||loan.PlazoMeses<=0||loan.Estado is not("VIGENTE" or "LIQUIDADO" or "VENCIDO" or "REESTRUCTURADO" or "CASTIGADO" or "CANCELADO"))throw Invalid("Revisa monto, plazo y estado del préstamo.");
            if(!id.HasValue)db.Add(loan);saved=loan.PrestamoId;break;
        case "desempeno":
            var loanId=Id(input,"prestamoId");if(!await db.Set<Prestamo>().AnyAsync(x=>x.EmpresaId==tenant&&x.PrestamoId==loanId,ct))throw ApplicationError.NotFound();
            var cut=id.HasValue?await db.Set<DesempenoCredito>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.PrestamoId==loanId&&x.DesempenoCreditoId==id,ct)??throw ApplicationError.NotFound():new DesempenoCredito{DesempenoCreditoId=Guid.NewGuid(),EmpresaId=tenant,PrestamoId=loanId};
            cut.FechaCorte=Date(input,"fechaCorte");cut.SaldoCapital=Number(input,"saldoCapital");cut.CuotaExigible=Number(input,"cuotaExigible");cut.MontoPagadoPeriodo=Number(input,"montoPagadoPeriodo");cut.DiasMora=Integer(input,"diasMora");cut.Refinanciado=Flag(input,"refinanciado");cut.Reestructurado=Flag(input,"reestructurado");cut.Castigado=Flag(input,"castigado");cut.EstadoCartera=Text(input,"estadoCartera",60,false);
            if(!id.HasValue)db.Add(cut);saved=cut.DesempenoCreditoId;break;
        case "rutas":
            var route=id.HasValue?(await db.Set<RutaAprobacion>().FromSqlInterpolated($"SELECT * FROM flujo.ruta_aprobacion WHERE empresa_id={tenant} AND ruta_aprobacion_id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound():new RutaAprobacion{RutaAprobacionId=Guid.NewGuid(),EmpresaId=tenant,Codigo=Text(input,"codigo",100),Nombre=Text(input,"nombre")};
            if(id.HasValue&&await db.Set<Aprobacion>().AnyAsync(x=>x.EmpresaId==tenant&&x.RutaAprobacionId==id,ct))throw Invalid("La ruta ya tiene aprobaciones históricas. Crea otra ruta para cambiar los pasos.");
            route.Codigo=Text(input,"codigo",100);route.Nombre=Text(input,"nombre");route.Prioridad=Integer(input,"prioridad");route.Activa=Flag(input,"activa");route.EsPredeterminada=Flag(input,"esPredeterminada");
            if(input.TryGetProperty("productoCreditoId",out var product)&&product.ValueKind==JsonValueKind.String&&Guid.TryParse(product.GetString(),out var productId)){if(!await db.Set<ProductoCredito>().AnyAsync(x=>x.EmpresaId==tenant&&x.ProductoCreditoId==productId,ct))throw ApplicationError.NotFound();route.ProductoCreditoId=productId;}
            if(!input.TryGetProperty("pasos",out var steps)||steps.ValueKind!=JsonValueKind.Array||steps.GetArrayLength()==0||steps.GetArrayLength()>30)throw Invalid("Configura entre uno y treinta pasos.");
            if(!id.HasValue)db.Add(route);else db.RemoveRange(await db.Set<RutaAprobacionPaso>().Where(x=>x.EmpresaId==tenant&&x.RutaAprobacionId==id).ToListAsync(ct));
            var order=0;foreach(var step in steps.EnumerateArray()){var role=Id(step,"rolId");if(!await db.Set<Rol>().AnyAsync(x=>x.EmpresaId==tenant&&x.RolId==role&&x.Estado=="ACTIVO",ct))throw ApplicationError.Forbidden();var count=Integer(step,"cantidadAprobacionesRequeridas");if(count<1)throw Invalid("Cada paso requiere al menos una aprobación.");db.Add(new RutaAprobacionPaso{RutaAprobacionPasoId=Guid.NewGuid(),EmpresaId=tenant,RutaAprobacionId=route.RutaAprobacionId,Orden=++order,Nombre=Text(step,"nombre"),RolId=role,CantidadAprobacionesRequeridas=count,Obligatorio=true,PermiteAprobar=true,PermiteRechazar=true,PermiteDevolver=true});}
            if(id.HasValue)db.RemoveRange(await db.Set<ReglaEnrutamiento>().Where(x=>x.EmpresaId==tenant&&x.RutaAprobacionId==id).ToListAsync(ct));
            if(!route.EsPredeterminada){var condition=Text(input,"condicionJson",65536);PolicyRepository.ValidateRule("ANALISIS",condition,"REVISION_ADICIONAL","INFO");db.Add(new ReglaEnrutamiento{ReglaEnrutamientoId=Guid.NewGuid(),EmpresaId=tenant,RutaAprobacionId=route.RutaAprobacionId,Codigo=route.Codigo[..Math.Min(route.Codigo.Length,80)]+"_RUTA",Nombre=route.Nombre,Prioridad=route.Prioridad,CondicionJson=condition,Activa=true});}
            saved=route.RutaAprobacionId;break;
        case "integraciones":
            var provider=Id(input,"proveedorId");if(!await db.Set<Proveedor>().AnyAsync(x=>x.ProveedorId==provider&&x.Activo,ct))throw ApplicationError.NotFound();
            var integration=id.HasValue?await db.Set<EmpresaProveedor>().SingleOrDefaultAsync(x=>x.EmpresaId==tenant&&x.EmpresaProveedorId==id,ct)??throw ApplicationError.NotFound():new EmpresaProveedor{EmpresaProveedorId=Guid.NewGuid(),EmpresaId=tenant,ProveedorId=provider,Ambiente="PRUEBAS"};
            integration.Ambiente=Text(input,"ambiente",30);if(integration.Ambiente is not("PRUEBAS" or "PRODUCCION"))throw Invalid("Ambiente no admitido.");integration.Activo=Flag(input,"activo");if(!id.HasValue)db.Add(integration);saved=integration.EmpresaProveedorId;break;
        default:throw ApplicationError.NotFound();
        }
        audit.Add(area,saved,id.HasValue?"ACTUALIZAR":"CREAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return saved;
    }
    private static ApplicationError Invalid(string message)=>new(422,"OPERATION_VALIDATION",message);
}
