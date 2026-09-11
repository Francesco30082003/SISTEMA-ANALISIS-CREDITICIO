using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Documentos;
using Mapan.Application.Integrations;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mapan.Infrastructure.Persistence.Repositories;
public sealed class DocumentoRepository(MapanDbContext db,AuditWriter audit,IFileStorage storage,IDocumentExtractionService extraction,ILogger<DocumentoRepository> logger):IDocumentoRepository
{
    // Best-effort: a document type without a known pattern, or a scanned file without a text layer,
    // must never block the upload itself — extraction is a convenience, capture manual is the source of truth.
    // Failures are still logged (never silently lost) so extraction problems are diagnosable in production.
    private async Task TryExtractAsync(Guid empresaId,Guid documentoVersionId,CancellationToken ct)
    {
        try{var result=await extraction.ExtractAsync(empresaId,documentoVersionId,ct);logger.LogInformation("Extracción de documento {DocumentoVersionId}: {Status}",documentoVersionId,result.Status);}
        catch(Exception e) when(e is not OperationCanceledException){logger.LogWarning(e,"Extracción de documento {DocumentoVersionId} falló",documentoVersionId);}
    }
    public async Task<DocumentReview> ReviewAsync(Guid empresaId,Guid id,CancellationToken ct)
    {
        var link=await db.Set<SolicitudDocumento>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.SolicitudDocumentoId==id,ct)??throw ApplicationError.NotFound();
        var extracted=await db.Set<DatoExtraido>().AsNoTracking().Where(x=>x.EmpresaId==empresaId&&x.DocumentoVersionId==link.DocumentoVersionId).OrderBy(x=>x.Pagina).ThenBy(x=>x.CodigoCampo).ToListAsync(ct);
        var validated=await db.Set<DatoValidado>().AsNoTracking().Where(x=>x.EmpresaId==empresaId&&x.SolicitudDocumentoId==id).OrderByDescending(x=>x.NumeroRevision).ToListAsync(ct);
        return new(extracted,validated);
    }
    public async Task<DocumentoDto> ReplaceAsync(Guid empresaId,Guid membershipId,Guid id,Stream content,string mime,CancellationToken ct)
    {
        var original=await db.Set<SolicitudDocumento>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.SolicitudDocumentoId==id,ct)??throw ApplicationError.NotFound();
        var prior=await db.Set<DocumentoVersion>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.DocumentoVersionId==original.DocumentoVersionId,ct)??throw ApplicationError.NotFound();
        var file=await storage.SaveAsync(empresaId,content,mime,ct);
        try {
            await using var tx=await db.Database.BeginTransactionAsync(ct);
            var parent=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={empresaId} AND solicitud_credito_id={original.SolicitudCreditoId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
            if(parent.Estado is not ("BORRADOR" or "DOCUMENTACION"))throw new ApplicationError(422,"INVALID_TRANSITION","La solicitud no admite nuevas versiones.");
            var document=(await db.Set<Documento>().FromSqlInterpolated($"SELECT * FROM documentos.documento WHERE empresa_id={empresaId} AND documento_id={prior.DocumentoId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
            var number=await db.Set<DocumentoVersion>().Where(x=>x.EmpresaId==empresaId&&x.DocumentoId==document.DocumentoId).MaxAsync(x=>x.NumeroVersion,ct);
            var version=new DocumentoVersion{DocumentoVersionId=Guid.NewGuid(),EmpresaId=empresaId,DocumentoId=document.DocumentoId,NumeroVersion=checked(number+1),NombreArchivo=file.FileName,MimeType=file.MimeType,TamanoBytes=file.Size,AlmacenamientoUri=file.Uri,HashSha256=file.Sha256,Origen="CARGA_MANUAL",CreadoPorUsuarioEmpresaId=membershipId};
            // Cada versión conserva su asociación y sus validaciones históricas.
            var link=new SolicitudDocumento{SolicitudDocumentoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=parent.SolicitudCreditoId,DocumentoVersionId=version.DocumentoVersionId,Estado="PENDIENTE",AsociadoPorUsuarioEmpresaId=membershipId};
            db.AddRange(version,link);audit.Add("documentos.documento_version",version.DocumentoVersionId,"NUEVA_VERSION");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            await TryExtractAsync(empresaId,version.DocumentoVersionId,ct);
            return new(link.SolicitudDocumentoId,version.DocumentoVersionId,document.Nombre,document.TipoDocumento,version.NumeroVersion,link.Estado,document.Estado,version.MimeType);
        }catch{await storage.RemoveUncommittedAsync(empresaId,file.Uri,CancellationToken.None);throw;}
    }
    public async Task<IReadOnlyList<DocumentoDto>> ListAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)=>await
        (from link in db.Set<SolicitudDocumento>().AsNoTracking() join version in db.Set<DocumentoVersion>() on link.DocumentoVersionId equals version.DocumentoVersionId
         join document in db.Set<Documento>() on version.DocumentoId equals document.DocumentoId
         where link.EmpresaId==empresaId&&link.SolicitudCreditoId==solicitudId&&version.EmpresaId==empresaId&&document.EmpresaId==empresaId
         orderby link.FechaAsociacion select new DocumentoDto(link.SolicitudDocumentoId,version.DocumentoVersionId,document.Nombre,document.TipoDocumento,version.NumeroVersion,link.Estado,document.Estado,version.MimeType)).ToListAsync(ct);
    public async Task<DocumentoDto> UploadAsync(Guid empresaId,Guid membershipId,Guid solicitudId,string nombre,string tipo,Stream content,string mime,CancellationToken ct)
    {
        var parent=await db.Set<SolicitudCredito>().AsNoTracking().SingleOrDefaultAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct)??throw ApplicationError.NotFound();
        var file=await storage.SaveAsync(empresaId,content,mime,ct);
        try {
            await using var tx=await db.Database.BeginTransactionAsync(ct);
            var locked=(await db.Set<SolicitudCredito>().FromSqlInterpolated($"SELECT * FROM credito.solicitud_credito WHERE empresa_id={empresaId} AND solicitud_credito_id={solicitudId} FOR UPDATE").ToListAsync(ct)).Single();
            if(locked.Estado is not ("BORRADOR" or "DOCUMENTACION"))throw new ApplicationError(422,"INVALID_TRANSITION","La solicitud no admite carga documental en su estado actual.");
            var document=new Documento{DocumentoId=Guid.NewGuid(),EmpresaId=empresaId,ClienteId=parent.ClienteId,Nombre=nombre,TipoDocumento=tipo,Estado="ACTIVO",CreadoPorUsuarioEmpresaId=membershipId};
            var version=new DocumentoVersion{DocumentoVersionId=Guid.NewGuid(),EmpresaId=empresaId,DocumentoId=document.DocumentoId,NumeroVersion=1,NombreArchivo=file.FileName,MimeType=file.MimeType,TamanoBytes=file.Size,AlmacenamientoUri=file.Uri,HashSha256=file.Sha256,Origen="CARGA_MANUAL",CreadoPorUsuarioEmpresaId=membershipId};
            var link=new SolicitudDocumento{SolicitudDocumentoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,DocumentoVersionId=version.DocumentoVersionId,Estado="PENDIENTE",AsociadoPorUsuarioEmpresaId=membershipId};
            db.AddRange(document,version,link);audit.Add("documentos.solicitud_documento",link.SolicitudDocumentoId,"CARGAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            await TryExtractAsync(empresaId,version.DocumentoVersionId,ct);
            return new(link.SolicitudDocumentoId,version.DocumentoVersionId,nombre,tipo,1,link.Estado,document.Estado,version.MimeType);
        }catch {await storage.RemoveUncommittedAsync(empresaId,file.Uri,CancellationToken.None);throw;}
    }
    public async Task<DocumentDownload> DownloadAsync(Guid empresaId,Guid versionId,CancellationToken ct)
    {
        var version=await db.Set<DocumentoVersion>().AsNoTracking().SingleOrDefaultAsync(v=>v.EmpresaId==empresaId&&v.DocumentoVersionId==versionId,ct)??throw ApplicationError.NotFound();
        return new(await storage.OpenAsync(empresaId,version.AlmacenamientoUri,ct),version.MimeType??"application/octet-stream",version.NombreArchivo);
    }
    public async Task<Guid> ValidateAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,ValidacionInput input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var link=(await db.Set<SolicitudDocumento>().FromSqlInterpolated($"SELECT * FROM documentos.solicitud_documento WHERE empresa_id={empresaId} AND solicitud_documento_id={solicitudDocumentoId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        if(input.DatoExtraidoId.HasValue&&!await db.Set<DatoExtraido>().AnyAsync(d=>d.EmpresaId==empresaId&&d.DocumentoVersionId==link.DocumentoVersionId&&d.DatoExtraidoId==input.DatoExtraidoId,ct))throw ApplicationError.NotFound();
        var last=await db.Set<DatoValidado>().Where(d=>d.EmpresaId==empresaId&&d.SolicitudDocumentoId==solicitudDocumentoId&&d.CodigoCampo==input.CodigoCampo).MaxAsync(d=>(int?)d.NumeroRevision,ct)??0;
        var data=new DatoValidado{DatoValidadoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudDocumentoId=solicitudDocumentoId,DatoExtraidoId=input.DatoExtraidoId,CodigoCampo=input.CodigoCampo,NumeroRevision=checked(last+1),TipoDato=input.TipoDato,ValorTexto=input.ValorTexto,ValorNumerico=input.ValorNumerico,ValorFecha=input.ValorFecha,ValorBooleano=input.ValorBooleano,ValorJson=input.ValorJson,Estado=input.Estado,Comentario=input.Comentario,ValidadoPorUsuarioEmpresaId=membershipId};
        db.Add(data);
        if(input.Estado=="CONFIRMADO")await ApplyAutofillAsync(empresaId,membershipId,link,input,ct);
        audit.Add("documentos.dato_validado",data.DatoValidadoId,"VALIDAR");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return data.DatoValidadoId;
    }
    public async Task VoidAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var link=await db.Set<SolicitudDocumento>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.SolicitudDocumentoId==solicitudDocumentoId,ct)??throw ApplicationError.NotFound();
        var parent=await db.Set<SolicitudCredito>().AsNoTracking().SingleOrDefaultAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==link.SolicitudCreditoId,ct)??throw ApplicationError.NotFound();
        if(parent.Estado is not ("BORRADOR" or "DOCUMENTACION"))throw new ApplicationError(422,"INVALID_TRANSITION","La solicitud no admite quitar documentos en su estado actual.");
        var version=await db.Set<DocumentoVersion>().AsNoTracking().SingleOrDefaultAsync(v=>v.EmpresaId==empresaId&&v.DocumentoVersionId==link.DocumentoVersionId,ct)??throw ApplicationError.NotFound();
        var document=(await db.Set<Documento>().FromSqlInterpolated($"SELECT * FROM documentos.documento WHERE empresa_id={empresaId} AND documento_id={version.DocumentoId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        if(document.Estado=="ANULADO"){await tx.CommitAsync(ct);return;}
        document.Estado="ANULADO";
        audit.Add("documentos.documento",document.DocumentoId,"ANULAR");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    // Un dato validado por el analista ya no debe volver a escribirse a mano en Actividad Económica /
    // Fuentes de Ingreso: si el campo confirmado pertenece a ese dominio, se aplica directamente sobre
    // la solicitud. Solo cubre los campos de documentos.dato_extraido con equivalente conocido; el
    // resto sigue siendo captura manual en el expediente.
    private async Task ApplyAutofillAsync(Guid empresaId,Guid membershipId,SolicitudDocumento link,ValidacionInput input,CancellationToken ct)
    {
        var solicitudCreditoId=link.SolicitudCreditoId;
        switch(input.CodigoCampo)
        {
            case "empleador" or "cargo" or "fecha_ingreso":
                var actividad=await db.Set<ActividadEconomica>().FirstOrDefaultAsync(a=>a.EmpresaId==empresaId&&a.SolicitudCreditoId==solicitudCreditoId&&a.EsPrincipal,ct);
                if(actividad is null)
                {
                    actividad=new ActividadEconomica{ActividadEconomicaId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudCreditoId,TipoActividad="DEPENDIENTE",EsPrincipal=true};
                    db.Add(actividad);
                }
                if(input.CodigoCampo=="empleador")actividad.EmpleadorNegocio=input.ValorTexto;
                else if(input.CodigoCampo=="cargo")actividad.CargoActividad=input.ValorTexto;
                else actividad.FechaInicio=input.ValorFecha;
                break;
            case "ingreso_mensual" when input.ValorNumerico.HasValue:
                var fuente=await db.Set<FuenteIngreso>().FirstOrDefaultAsync(f=>f.EmpresaId==empresaId&&f.SolicitudCreditoId==solicitudCreditoId&&f.TipoIngreso=="SALARIO",ct);
                if(fuente is null)
                {
                    fuente=new FuenteIngreso{FuenteIngresoId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudCreditoId,TipoIngreso="SALARIO",Descripcion="Ingreso mensual (documento validado)",MonedaCodigo="USD",EsRecurrente=true,Declarado=true};
                    db.Add(fuente);
                }
                var hoy=DateOnly.FromDateTime(DateTime.UtcNow);
                db.Add(new IngresoPeriodo{IngresoPeriodoId=Guid.NewGuid(),EmpresaId=empresaId,FuenteIngresoId=fuente.FuenteIngresoId,PeriodoInicio=new DateOnly(hoy.Year,hoy.Month,1),PeriodoFin=hoy,MontoNeto=input.ValorNumerico.Value,OrigenDato="DOCUMENTO_VALIDADO"});
                break;
            case var campo when campo.StartsWith("reporte_buro_"):
                await ApplyBuroReporteAutofillAsync(empresaId,membershipId,link,input,ct);
                break;
        }
    }
    // Fase 5: un reporte de buró/Equifax CARGADO (no consultado por API/mock) también termina en
    // integracion.buro_snapshot, para que InvestigacionRepository lo muestre igual que un resultado de
    // proveedor — solo cambia el origen (ReferenciaExterna=CARGA_DOCUMENTO, SolicitudDocumentoId con la
    // evidencia). Se reconstruye completo cada vez a partir de TODO lo confirmado de este documento,
    // así confirmar campos en momentos distintos nunca deja el snapshot a medio actualizar.
    private async Task ApplyBuroReporteAutofillAsync(Guid empresaId,Guid membershipId,SolicitudDocumento link,ValidacionInput input,CancellationToken ct)
    {
        var proveedorId=await (from ep in db.Set<EmpresaProveedor>().AsNoTracking() join p in db.Set<Proveedor>().AsNoTracking() on ep.ProveedorId equals p.ProveedorId
            where ep.EmpresaId==empresaId&&ep.Activo&&p.Activo&&p.Tipo=="BURO_CREDITO" orderby ep.FechaCreacion select (Guid?)ep.EmpresaProveedorId).FirstOrDefaultAsync(ct);
        if(proveedorId is null)return; // sin proveedor de buró configurado, no hay a quién atribuir el reporte.

        // El campo que se confirma justo ahora (input) todavía no está guardado en dato_validado —
        // db.Add(data) en ValidateAsync se hace antes de llamar aquí, pero SaveChangesAsync ocurre
        // después — así que se fusiona a mano; si solo se leyera de la base, la confirmación actual
        // quedaría siempre "un paso atrás" del snapshot reconstruido.
        var confirmados=await db.Set<DatoValidado>().AsNoTracking()
            .Where(d=>d.EmpresaId==empresaId&&d.SolicitudDocumentoId==link.SolicitudDocumentoId&&d.Estado=="CONFIRMADO"&&d.CodigoCampo.StartsWith("reporte_buro_")&&d.CodigoCampo!=input.CodigoCampo)
            .GroupBy(d=>d.CodigoCampo).Select(g=>g.OrderByDescending(d=>d.NumeroRevision).First()).ToListAsync(ct);
        var byCode=confirmados.ToDictionary(d=>d.CodigoCampo,d=>(Numero:d.ValorNumerico,Fecha:d.ValorFecha));
        byCode[input.CodigoCampo]=(input.ValorNumerico,input.ValorFecha);
        decimal? Num(string c)=>byCode.TryGetValue(c,out var d)?d.Numero:null;

        // Campos que un reporte cargado no reporta (probabilidad de mora, desglose por fuente, mora
        // histórica) quedan sin dato — nunca se inventan solo para completar la forma del registro.
        var resultado=new BuroCreditoResultado(
            Num("reporte_buro_score").HasValue?(int)Num("reporte_buro_score")!.Value:null,null,null,
            Num("reporte_buro_deuda_total")??0m,Num("reporte_buro_cuota_total")??0m,
            (int)(Num("reporte_buro_operaciones_vencidas")??0m),0,
            Num("reporte_buro_monto_demanda_judicial")??0m,Num("reporte_buro_monto_cartera_castigada")??0m,
            Num("reporte_buro_mora_actual_dias").HasValue?(int)Num("reporte_buro_mora_actual_dias")!.Value:null,null,[]);
        var fechaReporte=byCode.TryGetValue("reporte_buro_fecha_reporte",out var fechaDato)?fechaDato.Fecha:null;
        var json=JsonSerializer.Serialize(resultado);

        var snapshot=await db.Set<BuroSnapshot>().SingleOrDefaultAsync(b=>b.EmpresaId==empresaId&&b.SolicitudDocumentoId==link.SolicitudDocumentoId,ct);
        if(snapshot is null)
        {
            var consulta=new ConsultaExterna{ConsultaExternaId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=link.SolicitudCreditoId,EmpresaProveedorId=proveedorId.Value,
                TipoConsulta="BURO_CREDITO",Estado="COMPLETADA",ReferenciaExterna="CARGA_DOCUMENTO",IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=DateTimeOffset.UtcNow,FechaFin=DateTimeOffset.UtcNow};
            db.Add(consulta);
            audit.Add("integracion.consulta_externa",consulta.ConsultaExternaId,"CARGAR_REPORTE_BURO");
            snapshot=new BuroSnapshot{BuroSnapshotId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=link.SolicitudCreditoId,EmpresaProveedorId=proveedorId.Value,
                ConsultaExternaId=consulta.ConsultaExternaId,SolicitudDocumentoId=link.SolicitudDocumentoId};
            db.Add(snapshot);
        }
        snapshot.ScoreBuro=(decimal?)resultado.Score;snapshot.DeudaTotal=resultado.DeudaTotal;snapshot.CuotaTotal=resultado.CuotaTotalMensual;
        snapshot.MoraActualMaxDias=resultado.MoraActualMaxDias;snapshot.FechaReporte=fechaReporte;snapshot.PayloadNormalizado=json;
        audit.Add("integracion.buro_snapshot",snapshot.BuroSnapshotId,"ACTUALIZAR_DESDE_REPORTE");
    }
}
