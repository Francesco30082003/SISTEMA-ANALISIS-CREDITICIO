using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Integrations;
using Mapan.Application.Verificacion;
using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Infrastructure.Persistence.Repositories;

// Verificación documental y laboral (Fase 6): igual que InvestigacionRepository, cada fuente se resuelve
// por categoría de Proveedor.Tipo (no por vendor fijo). A diferencia de la investigación crediticia (que
// ejecuta las 3 fuentes juntas en un solo botón), aquí cada verificación es una acción explícita por
// documento/solicitud — si no hay proveedor activo, se informa con un error claro en vez de fallar en silencio.
public sealed class VerificacionRepository(MapanDbContext db,AuditWriter audit,IDocumentVerificationProvider docProvider,
    IVerificacionLaboralProvider laboralProvider,TimeProvider clock) : IVerificacionRepository
{
    public async Task<IReadOnlyList<DocumentoVerificacionDto>> ListarDocumentosAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        var documentos = await
            (from link in db.Set<SolicitudDocumento>().AsNoTracking()
             join version in db.Set<DocumentoVersion>().AsNoTracking() on link.DocumentoVersionId equals version.DocumentoVersionId
             join document in db.Set<Documento>().AsNoTracking() on version.DocumentoId equals document.DocumentoId
             where link.EmpresaId==empresaId&&link.SolicitudCreditoId==solicitudId&&version.EmpresaId==empresaId&&document.EmpresaId==empresaId&&document.Estado=="ACTIVO"
             orderby link.FechaAsociacion
             select new { link.SolicitudDocumentoId,document.DocumentoId,document.Nombre,document.TipoDocumento,document.EstadoVerificacion }
            ).ToListAsync(ct);
        if(documentos.Count==0)return [];

        var documentoIds = documentos.Select(d=>d.DocumentoId).Distinct().ToList();
        var ultimas = await db.Set<VerificacionDocumento>().AsNoTracking().Where(v=>v.EmpresaId==empresaId&&documentoIds.Contains(v.DocumentoId))
            .GroupBy(v=>v.DocumentoId).Select(g=>g.OrderByDescending(v=>v.FechaCreacion).First()).ToListAsync(ct);
        var ultimaPorDocumento = ultimas.ToDictionary(v=>v.DocumentoId);

        return documentos.Select(d=>
        {
            ultimaPorDocumento.TryGetValue(d.DocumentoId,out var v);
            var motivos = v?.MotivosJson is not null ? JsonSerializer.Deserialize<IReadOnlyList<string>>(v.MotivosJson) : null;
            return new DocumentoVerificacionDto(d.SolicitudDocumentoId,d.DocumentoId,d.Nombre,d.TipoDocumento,d.EstadoVerificacion,v?.ConfianzaPct,motivos,v?.FechaCreacion);
        }).ToList();
    }

    public async Task<DocumentoVerificacionDto> VerificarDocumentoAsync(Guid empresaId,Guid membershipId,Guid solicitudDocumentoId,CancellationToken ct)
    {
        var empresaProveedorId = await ResolveProviderAsync(empresaId,"VERIFICACION_DOCUMENTAL",ct)
            ?? throw new ApplicationError(422,"PROVEEDOR_NO_CONFIGURADO","No hay un proveedor de verificación documental activo para esta empresa.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var link = await db.Set<SolicitudDocumento>().AsNoTracking().SingleOrDefaultAsync(x=>x.EmpresaId==empresaId&&x.SolicitudDocumentoId==solicitudDocumentoId,ct)??throw ApplicationError.NotFound();
        var version = await db.Set<DocumentoVersion>().AsNoTracking().SingleOrDefaultAsync(v=>v.EmpresaId==empresaId&&v.DocumentoVersionId==link.DocumentoVersionId,ct)??throw ApplicationError.NotFound();
        var document = (await db.Set<Documento>().FromSqlInterpolated($"SELECT * FROM documentos.documento WHERE empresa_id={empresaId} AND documento_id={version.DocumentoId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw ApplicationError.NotFound();
        if(document.Estado=="ANULADO")throw new ApplicationError(422,"INVALID_TRANSITION","El documento fue anulado y no puede verificarse.");

        var resultado = await docProvider.VerificarAsync(document.TipoDocumento,version.NombreArchivo,version.TamanoBytes??0,ct);
        var entity = new VerificacionDocumento{VerificacionId=Guid.NewGuid(),EmpresaId=empresaId,DocumentoId=document.DocumentoId,EmpresaProveedorId=empresaProveedorId,
            Resultado=resultado.Resultado,ConfianzaPct=resultado.ConfianzaPct,MotivosJson=JsonSerializer.Serialize(resultado.Motivos),
            EjecutadaPorUsuarioEmpresaId=membershipId,FechaCreacion=clock.GetUtcNow()};
        db.Add(entity);
        document.EstadoVerificacion = resultado.Resultado;
        audit.Add("documentos.verificacion",entity.VerificacionId,"VERIFICAR");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);

        return new(solicitudDocumentoId,document.DocumentoId,document.Nombre,document.TipoDocumento,document.EstadoVerificacion,resultado.ConfianzaPct,resultado.Motivos,entity.FechaCreacion);
    }

    public async Task<VerificacionLaboralDto?> GetUltimaLaboralAsync(Guid empresaId,Guid solicitudId,CancellationToken ct)
    {
        if(!await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct))
            throw ApplicationError.NotFound();
        var snapshot = await db.Set<IessSnapshot>().AsNoTracking().Where(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId)
            .OrderByDescending(s=>s.FechaCreacion).FirstOrDefaultAsync(ct);
        return snapshot is null?null:new(snapshot.RelacionLaboralActiva,snapshot.EmpleadorRegistrado,snapshot.FechaAfiliacion,snapshot.AporteMensual,snapshot.Estado,snapshot.FechaCreacion);
    }

    public async Task<VerificacionLaboralDto> EjecutarLaboralAsync(Guid empresaId,Guid membershipId,Guid solicitudId,CancellationToken ct)
    {
        var empresaProveedorId = await ResolveProviderAsync(empresaId,"VERIFICACION_LABORAL",ct)
            ?? throw new ApplicationError(422,"PROVEEDOR_NO_CONFIGURADO","No hay un proveedor de verificación laboral (IESS) activo para esta empresa.");
        var solicitud = await db.Set<SolicitudCredito>().AsNoTracking().SingleOrDefaultAsync(s=>s.EmpresaId==empresaId&&s.SolicitudCreditoId==solicitudId,ct)??throw ApplicationError.NotFound();
        var cliente = await db.Set<Cliente>().AsNoTracking().SingleOrDefaultAsync(c=>c.EmpresaId==empresaId&&c.ClienteId==solicitud.ClienteId,ct)??throw ApplicationError.NotFound();

        var resultado = await laboralProvider.ConsultarAsync(cliente.NumeroIdentificacion,ct);
        var ahora = clock.GetUtcNow();
        var consulta = new ConsultaExterna{ConsultaExternaId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId,
            TipoConsulta="VERIFICACION_LABORAL",Estado="COMPLETADA",ReferenciaExterna="CONSULTA_API",RespuestaResumenJson=JsonSerializer.Serialize(resultado),
            IniciadaPorUsuarioEmpresaId=membershipId,FechaInicio=ahora,FechaFin=clock.GetUtcNow()};
        db.Add(consulta);
        var snapshot = new IessSnapshot{IessSnapshotId=Guid.NewGuid(),EmpresaId=empresaId,SolicitudCreditoId=solicitudId,EmpresaProveedorId=empresaProveedorId,
            ConsultaExternaId=consulta.ConsultaExternaId,RelacionLaboralActiva=resultado.RelacionLaboralActiva,EmpleadorRegistrado=resultado.EmpleadorRegistrado,
            FechaAfiliacion=resultado.FechaAfiliacion,AporteMensual=resultado.AporteMensual,Estado=resultado.Estado,
            PayloadNormalizado=JsonSerializer.Serialize(resultado),FechaCreacion=ahora};
        db.Add(snapshot);
        audit.Add("integracion.consulta_externa",consulta.ConsultaExternaId,"CONSULTAR_IESS");
        audit.Add("integracion.iess_snapshot",snapshot.IessSnapshotId,"CONSULTAR_IESS");
        await db.SaveChangesAsync(ct);

        return new(snapshot.RelacionLaboralActiva,snapshot.EmpleadorRegistrado,snapshot.FechaAfiliacion,snapshot.AporteMensual,snapshot.Estado,snapshot.FechaCreacion);
    }

    private async Task<Guid?> ResolveProviderAsync(Guid empresaId,string tipo,CancellationToken ct)=>
        await (from ep in db.Set<EmpresaProveedor>().AsNoTracking()
               join p in db.Set<Proveedor>().AsNoTracking() on ep.ProveedorId equals p.ProveedorId
               where ep.EmpresaId==empresaId&&ep.Activo&&p.Activo&&p.Tipo==tipo
               orderby ep.FechaCreacion
               select (Guid?)ep.EmpresaProveedorId).FirstOrDefaultAsync(ct);
}
