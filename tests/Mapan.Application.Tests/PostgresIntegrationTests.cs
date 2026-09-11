using System.Text.Json;
using Mapan.Application.Common;
using Mapan.Application.Documentos;
using Mapan.Application.Integrations;
using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Integrations;
using Mapan.Infrastructure.Persistence;
using Mapan.Infrastructure.Persistence.Repositories;
using Mapan.Infrastructure.Security;
using Mapan.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapan.Application.Tests;

// Runs against the REAL PostgreSQL configured in User Secrets (same UserSecretsId as Mapan.Api),
// because SecurityAdministrationRepository.AssignRolesAsync uses a raw "FOR UPDATE" lock that
// EF Core's InMemory provider does not support. Writes and deletes clearly synthetic rows
// (ZZTEST- prefixed) and verifies, at the end, that nothing synthetic remains. Skips silently
// (no assertion) when no connection string is configured, e.g. in an environment without DB access.
public sealed class PostgresIntegrationTests
{
    private static string? ConnectionString() =>
        new ConfigurationBuilder().AddUserSecrets("9bfb2d54-70d6-4606-8ce4-2d49195d2511").AddEnvironmentVariables().Build()
            .GetConnectionString("MapanDatabase");

    [Fact]
    public async Task AssignRolesAsync_RejectsCrossTenantRole_AndPersistsOnlyTheValidOne_OnRealPostgres()
    {
        var connectionString = ConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return; // no DB access in this environment: skip, don't fail.

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options);

        var ownCompany = new Empresa { EmpresaId = Guid.NewGuid(), Codigo = "ZZTEST-OWN-" + suffix, NombreLegal = "ZZTEST empresa propia", PaisCodigo = "EC", MonedaCodigo = "USD", Estado = "ACTIVA" };
        var otherCompany = new Empresa { EmpresaId = Guid.NewGuid(), Codigo = "ZZTEST-OTHER-" + suffix, NombreLegal = "ZZTEST empresa ajena", PaisCodigo = "EC", MonedaCodigo = "USD", Estado = "ACTIVA" };
        var user = new Usuario { UsuarioId = Guid.NewGuid(), NombreUsuario = "zztest_" + suffix, Correo = "zztest_" + suffix + "@example.invalid", PasswordHash = "unused", Nombres = "ZZTEST", Apellidos = "Integration", Estado = "ACTIVO" };
        var membership = new UsuarioEmpresa { UsuarioEmpresaId = Guid.NewGuid(), UsuarioId = user.UsuarioId, EmpresaId = ownCompany.EmpresaId, Estado = "ACTIVO" };
        var ownRole = new Rol { RolId = Guid.NewGuid(), EmpresaId = ownCompany.EmpresaId, Codigo = "ZZTEST-OWNROLE-" + suffix, Nombre = "ZZTEST rol propio", Estado = "ACTIVO" };
        var otherRole = new Rol { RolId = Guid.NewGuid(), EmpresaId = otherCompany.EmpresaId, Codigo = "ZZTEST-OTHERROLE-" + suffix, Nombre = "ZZTEST rol ajeno", Estado = "ACTIVO" };

        try
        {
            db.AddRange(ownCompany, otherCompany, user, membership, ownRole, otherRole);
            await db.SaveChangesAsync();

            var current = new FakeContext(user.UsuarioId, ownCompany.EmpresaId, membership.UsuarioEmpresaId);
            var repository = new SecurityAdministrationRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System), new PasswordService());

            var error = await Assert.ThrowsAsync<ApplicationError>(() => repository.AssignRolesAsync(ownCompany.EmpresaId, membership.UsuarioEmpresaId, [otherRole.RolId], default));
            Assert.Equal(403, error.Status);
            Assert.False(await db.Set<UsuarioEmpresaRol>().AsNoTracking().AnyAsync(x => x.UsuarioEmpresaId == membership.UsuarioEmpresaId),
                "El rechazo por cruce de empresa no debe dejar ninguna fila escrita, ni siquiera parcial.");

            await repository.AssignRolesAsync(ownCompany.EmpresaId, membership.UsuarioEmpresaId, [ownRole.RolId], default);
            Assert.True(await db.Set<UsuarioEmpresaRol>().AsNoTracking().AnyAsync(x => x.UsuarioEmpresaId == membership.UsuarioEmpresaId && x.RolId == ownRole.RolId),
                "Asignar un rol de la propia empresa sí debe persistir la fila.");
        }
        finally
        {
            // Explicit, exact-id cleanup — never pattern-based — so this can never touch real business data.
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM auditoria.evento WHERE empresa_id IN ({ownCompany.EmpresaId},{otherCompany.EmpresaId})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario_empresa_rol WHERE usuario_empresa_id={membership.UsuarioEmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.rol WHERE rol_id IN ({ownRole.RolId},{otherRole.RolId})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario_empresa WHERE usuario_empresa_id={membership.UsuarioEmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario WHERE usuario_id={user.UsuarioId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.empresa WHERE empresa_id IN ({ownCompany.EmpresaId},{otherCompany.EmpresaId})");

            Assert.False(await db.Set<Empresa>().AsNoTracking().AnyAsync(x => x.EmpresaId == ownCompany.EmpresaId || x.EmpresaId == otherCompany.EmpresaId),
                "La limpieza debe dejar la base exactamente como estaba: sin residuo sintético.");
        }
    }

    [Fact]
    public async Task FullCreditFlow_ClienteToWorkflow_RunsOnRealPostgresWithoutFabricatingMl()
    {
        var connectionString = ConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options);
        var now = DateTimeOffset.UtcNow;

        var company = new Empresa { EmpresaId = Guid.NewGuid(), Codigo = "ZZTEST-FLOW-" + suffix, NombreLegal = "ZZTEST flujo integral", PaisCodigo = "EC", MonedaCodigo = "USD", Estado = "ACTIVA" };
        var branch = new Sucursal { SucursalId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-SUC-" + suffix, Nombre = "ZZTEST sucursal", PaisCodigo = "EC", Estado = "ACTIVA" };
        var user = new Usuario { UsuarioId = Guid.NewGuid(), NombreUsuario = "zztestflow_" + suffix, Correo = "zztestflow_" + suffix + "@example.invalid", PasswordHash = "unused", Nombres = "ZZTEST", Apellidos = "Flow", Estado = "ACTIVO" };
        var membership = new UsuarioEmpresa { UsuarioEmpresaId = Guid.NewGuid(), UsuarioId = user.UsuarioId, EmpresaId = company.EmpresaId, Estado = "ACTIVO" };
        var role = new Rol { RolId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-ROL-" + suffix, Nombre = "ZZTEST aprobador", Estado = "ACTIVO" };
        var product = new ProductoCredito { ProductoCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-PROD-" + suffix, Nombre = "ZZTEST producto", MonedaCodigo = "USD", Estado = "ACTIVO" };
        var client = new Cliente { ClienteId = Guid.NewGuid(), EmpresaId = company.EmpresaId, TipoPersona = "NATURAL", TipoIdentificacion = "CEDULA", NumeroIdentificacion = "ZZTEST-" + suffix, Nombres = "ZZTEST", Apellidos = "Cliente", Estado = "ACTIVO" };
        var policy = new PoliticaCredito { PoliticaCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-POL-" + suffix, Nombre = "ZZTEST política", Estado = "ACTIVA" };
        var version = new PoliticaVersion { PoliticaVersionId = Guid.NewGuid(), EmpresaId = company.EmpresaId, PoliticaCreditoId = policy.PoliticaCreditoId, NumeroVersion = 1, VigenteDesde = now.AddDays(-1), Estado = "VIGENTE", CreadaPorUsuarioEmpresaId = membership.UsuarioEmpresaId };
        var factor = new Parametro { ParametroId = Guid.NewGuid(), EmpresaId = company.EmpresaId, PoliticaVersionId = version.PoliticaVersionId, Codigo = "FACTOR_CAPACIDAD", Nombre = "Factor de capacidad", TipoDato = "NUMERO", ValorNumerico = 0.5m };
        var request = new SolicitudCredito { SolicitudCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SucursalId = branch.SucursalId, ClienteId = client.ClienteId, ProductoCreditoId = product.ProductoCreditoId, NumeroSolicitud = "ZZTEST-SOL-" + suffix, MontoSolicitado = 5000m, PlazoSolicitadoMeses = 12, CuotaEstimada = 400m, Estado = "BORRADOR", CreadoPorUsuarioEmpresaId = membership.UsuarioEmpresaId };
        var source = new FuenteIngreso { FuenteIngresoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SolicitudCreditoId = request.SolicitudCreditoId, TipoIngreso = "DEPENDENCIA", MonedaCodigo = "USD", EsRecurrente = true, Declarado = true };
        var period = new IngresoPeriodo { IngresoPeriodoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, FuenteIngresoId = source.FuenteIngresoId, PeriodoInicio = new DateOnly(2026, 8, 1), PeriodoFin = new DateOnly(2026, 8, 31), MontoNeto = 1500m };
        var expense = new Gasto { GastoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SolicitudCreditoId = request.SolicitudCreditoId, TipoGasto = "VIVIENDA", MontoMensual = 300m, Declarado = true };
        var debt = new Obligacion { ObligacionId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SolicitudCreditoId = request.SolicitudCreditoId, Institucion = "ZZTEST banco", TipoObligacion = "CONSUMO", SaldoActual = 500m, CuotaMensual = 50m, Estado = "VIGENTE" };
        var route = new RutaAprobacion { RutaAprobacionId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-RUTA-" + suffix, Nombre = "ZZTEST ruta predeterminada", EsPredeterminada = true, Activa = true };
        var step = new RutaAprobacionPaso { RutaAprobacionPasoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, RutaAprobacionId = route.RutaAprobacionId, Orden = 1, Nombre = "ZZTEST revisión", RolId = role.RolId, CantidadAprobacionesRequeridas = 1, PermiteAprobar = true, PermiteRechazar = true, PermiteDevolver = true };

        try
        {
            db.AddRange(company, branch, user, membership, role, product, client, policy, version, factor, request, source, period, expense, debt, route, step);
            await db.SaveChangesAsync();

            var current = new FakeContext(user.UsuarioId, company.EmpresaId, membership.UsuarioEmpresaId);
            var repository = new CreditAnalysisRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System), new NeverCalledRiskModelClient(), new NoOpEmailSender(), NullLogger<CreditAnalysisRepository>.Instance);
            var analysisId = await repository.ExecuteAsync(company.EmpresaId, membership.UsuarioEmpresaId, request.SolicitudCreditoId, default);

            var analysis = await db.Set<Analisis>().AsNoTracking().SingleAsync(x => x.AnalisisId == analysisId);
            Assert.Equal("COMPLETADO", analysis.Estado);

            var snapshot = await db.Set<SnapshotFinanciero>().AsNoTracking().SingleAsync(x => x.AnalisisId == analysisId);
            Assert.Equal(1500m, snapshot.IngresoTotalMensual);
            Assert.Equal(300m, snapshot.GastoTotalMensual);
            Assert.Equal(600m, snapshot.CapacidadNuevaCuota);
            Assert.True(snapshot.CuotaCompatible);

            var vector = await db.Set<VectorCaracteristicas>().AsNoTracking().SingleAsync(x => x.AnalisisId == analysisId);
            Assert.Equal(1, vector.CreditosActivos); // VIGENTE counts as active per the 2026-09-06 decision.
            Assert.Null(vector.EstabilidadIngresosScore); // decided to stay NULL indefinitely.
            Assert.Null(vector.HistorialInternoScore);

            var recommendation = await db.Set<Recomendacion>().AsNoTracking().SingleAsync(x => x.AnalisisId == analysisId);
            Assert.Equal("CAPACIDAD_COMPATIBLE", recommendation.CodigoRecomendacion);
            Assert.True(recommendation.RequiereRevisionHumana); // never auto-approves.
            Assert.Null(recommendation.PrediccionId); // no ML model registered: must not fabricate a prediction.
            using (var detail = JsonDocument.Parse(recommendation.DetalleJson!))
                Assert.Equal("MODELO_PREDICTIVO_NO_CONFIGURADO", detail.RootElement.GetProperty("mlStatus").GetString());

            var approval = await db.Set<Aprobacion>().AsNoTracking().SingleAsync(x => x.RecomendacionId == recommendation.RecomendacionId);
            Assert.Equal("EN_CURSO", approval.Estado);
            var approvalStep = await db.Set<AprobacionPaso>().AsNoTracking().SingleAsync(x => x.AprobacionId == approval.AprobacionId);
            Assert.Equal("ACTIVO", approvalStep.Estado);

            var updatedRequest = await db.Set<SolicitudCredito>().AsNoTracking().SingleAsync(x => x.SolicitudCreditoId == request.SolicitudCreditoId);
            Assert.Equal("REVISION", updatedRequest.Estado);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM auditoria.evento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM flujo.aprobacion_paso WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM flujo.aprobacion WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM flujo.ruta_aprobacion_paso WHERE ruta_aprobacion_paso_id={step.RutaAprobacionPasoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM flujo.ruta_aprobacion WHERE ruta_aprobacion_id={route.RutaAprobacionId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM riesgo.recomendacion WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM riesgo.alerta WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM riesgo.vector_caracteristicas WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM riesgo.snapshot_financiero WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM riesgo.analisis WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.obligacion WHERE obligacion_id={debt.ObligacionId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.gasto WHERE gasto_id={expense.GastoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.ingreso_periodo WHERE ingreso_periodo_id={period.IngresoPeriodoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.fuente_ingreso WHERE fuente_ingreso_id={source.FuenteIngresoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.solicitud_credito WHERE solicitud_credito_id={request.SolicitudCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM politica.parametro WHERE parametro_id={factor.ParametroId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM politica.politica_version WHERE politica_version_id={version.PoliticaVersionId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM politica.politica_credito WHERE politica_credito_id={policy.PoliticaCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.producto_credito WHERE producto_credito_id={product.ProductoCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.cliente WHERE cliente_id={client.ClienteId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.sucursal WHERE sucursal_id={branch.SucursalId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario_empresa WHERE usuario_empresa_id={membership.UsuarioEmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.rol WHERE rol_id={role.RolId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario WHERE usuario_id={user.UsuarioId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.empresa WHERE empresa_id={company.EmpresaId}");

            Assert.False(await db.Set<Empresa>().AsNoTracking().AnyAsync(x => x.EmpresaId == company.EmpresaId),
                "La limpieza debe dejar la base exactamente como estaba: sin residuo sintético.");
        }
    }
    [Fact]
    public async Task UploadingASampleCertificateActuallyExtractsFieldsOnRealPostgresAndRealStorage()
    {
        var connectionString = ConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options);
        var company = new Empresa { EmpresaId = Guid.NewGuid(), Codigo = "ZZTEST-DOC-" + suffix, NombreLegal = "ZZTEST documentos", PaisCodigo = "EC", MonedaCodigo = "USD", Estado = "ACTIVA" };
        var branch = new Sucursal { SucursalId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-SUC-" + suffix, Nombre = "ZZTEST sucursal", PaisCodigo = "EC", Estado = "ACTIVA" };
        var user = new Usuario { UsuarioId = Guid.NewGuid(), NombreUsuario = "zztestdoc_" + suffix, Correo = "zztestdoc_" + suffix + "@example.invalid", PasswordHash = "unused", Nombres = "ZZTEST", Apellidos = "Doc", Estado = "ACTIVO" };
        var membership = new UsuarioEmpresa { UsuarioEmpresaId = Guid.NewGuid(), UsuarioId = user.UsuarioId, EmpresaId = company.EmpresaId, Estado = "ACTIVO" };
        var product = new ProductoCredito { ProductoCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-PROD-" + suffix, Nombre = "ZZTEST producto", MonedaCodigo = "USD", Estado = "ACTIVO" };
        var client = new Cliente { ClienteId = Guid.NewGuid(), EmpresaId = company.EmpresaId, TipoPersona = "NATURAL", TipoIdentificacion = "CEDULA", NumeroIdentificacion = "ZZTEST-" + suffix, Nombres = "ZZTEST", Apellidos = "Cliente", Estado = "ACTIVO" };
        var request = new SolicitudCredito { SolicitudCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SucursalId = branch.SucursalId, ClienteId = client.ClienteId, ProductoCreditoId = product.ProductoCreditoId, NumeroSolicitud = "ZZTEST-SOL-" + suffix, MontoSolicitado = 1000m, PlazoSolicitadoMeses = 6, Estado = "BORRADOR", CreadoPorUsuarioEmpresaId = membership.UsuarioEmpresaId };
        var tempRoot = Path.Combine(Path.GetTempPath(), "mapan-test-storage-" + suffix);

        try
        {
            db.AddRange(company, branch, user, membership, product, client, request);
            await db.SaveChangesAsync();

            var current = new FakeContext(user.UsuarioId, company.EmpresaId, membership.UsuarioEmpresaId);
            var storage = new LocalFileStorage(new FileStorageSettings(tempRoot, 20 * 1024 * 1024, new HashSet<string> { "application/pdf" }));
            var extraction = new TextPdfDocumentExtractionService(db, storage);
            var repository = new DocumentoRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System), storage, extraction, NullLogger<DocumentoRepository>.Instance);

            using var stream = new MemoryStream(DocumentExtractionPatternTests.BuildSampleCertificadoIngresos());
            var uploaded = await repository.UploadAsync(company.EmpresaId, membership.UsuarioEmpresaId, request.SolicitudCreditoId, "Certificado de prueba", "CERTIFICADO_INGRESOS", stream, "application/pdf", default);

            var extracted = await db.Set<DatoExtraido>().AsNoTracking().Where(x => x.EmpresaId == company.EmpresaId && x.DocumentoVersionId == uploaded.DocumentoVersionId).ToListAsync();
            Assert.Equal(8, extracted.Count); // the 8 fields registered for CERTIFICADO_INGRESOS.
            Assert.Contains(extracted, x => x.CodigoCampo == "ingreso_mensual" && x.ValorNumerico == 1450.00m);
            Assert.Contains(extracted, x => x.CodigoCampo == "empleador" && x.ValorTexto == "Textiles Andinos S.A.");

            var review = await new DocumentoRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System), storage, extraction, NullLogger<DocumentoRepository>.Instance)
                .ReviewAsync(company.EmpresaId, uploaded.SolicitudDocumentoId, default);
            Assert.Equal(8, review.Extraidos.Count);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM auditoria.evento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.dato_extraido WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.solicitud_documento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.documento_version WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.documento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.solicitud_credito WHERE solicitud_credito_id={request.SolicitudCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.producto_credito WHERE producto_credito_id={product.ProductoCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.cliente WHERE cliente_id={client.ClienteId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.sucursal WHERE sucursal_id={branch.SucursalId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario_empresa WHERE usuario_empresa_id={membership.UsuarioEmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario WHERE usuario_id={user.UsuarioId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.empresa WHERE empresa_id={company.EmpresaId}");
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);

            Assert.False(await db.Set<Empresa>().AsNoTracking().AnyAsync(x => x.EmpresaId == company.EmpresaId),
                "La limpieza debe dejar la base exactamente como estaba: sin residuo sintético.");
        }
    }

    // Fase 5: confirma los campos reporte_buro_* de a uno por vez (llamadas separadas a ValidateAsync,
    // cada una con su propio SaveChanges — igual que ocurriría en la UI real, campo por campo) y
    // verifica que integracion.buro_snapshot termine coherente con TODOS los valores, no solo el
    // último. Esta es la prueba de regresión del bug real encontrado en verificación manual: la
    // reconstrucción del snapshot leía dato_validado desde la base ANTES de que la confirmación actual
    // se guardara, así que cada confirmación quedaba "un paso atrás" del snapshot reconstruido.
    [Fact]
    public async Task ConfirmingBuroReportFieldsOneAtATimeStillBuildsACoherentSnapshot()
    {
        var connectionString = ConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options);
        var company = new Empresa { EmpresaId = Guid.NewGuid(), Codigo = "ZZTEST-BURO-" + suffix, NombreLegal = "ZZTEST buró", PaisCodigo = "EC", MonedaCodigo = "USD", Estado = "ACTIVA" };
        var branch = new Sucursal { SucursalId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-SUC-" + suffix, Nombre = "ZZTEST sucursal", PaisCodigo = "EC", Estado = "ACTIVA" };
        var user = new Usuario { UsuarioId = Guid.NewGuid(), NombreUsuario = "zztestburo_" + suffix, Correo = "zztestburo_" + suffix + "@example.invalid", PasswordHash = "unused", Nombres = "ZZTEST", Apellidos = "Buró", Estado = "ACTIVO" };
        var membership = new UsuarioEmpresa { UsuarioEmpresaId = Guid.NewGuid(), UsuarioId = user.UsuarioId, EmpresaId = company.EmpresaId, Estado = "ACTIVO" };
        var product = new ProductoCredito { ProductoCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, Codigo = "ZZTEST-PROD-" + suffix, Nombre = "ZZTEST producto", MonedaCodigo = "USD", Estado = "ACTIVO" };
        var client = new Cliente { ClienteId = Guid.NewGuid(), EmpresaId = company.EmpresaId, TipoPersona = "NATURAL", TipoIdentificacion = "CEDULA", NumeroIdentificacion = "ZZTEST-" + suffix, Nombres = "ZZTEST", Apellidos = "Cliente", Estado = "ACTIVO" };
        var request = new SolicitudCredito { SolicitudCreditoId = Guid.NewGuid(), EmpresaId = company.EmpresaId, SucursalId = branch.SucursalId, ClienteId = client.ClienteId, ProductoCreditoId = product.ProductoCreditoId, NumeroSolicitud = "ZZTEST-SOL-" + suffix, MontoSolicitado = 1000m, PlazoSolicitadoMeses = 6, Estado = "BORRADOR", CreadoPorUsuarioEmpresaId = membership.UsuarioEmpresaId };
        var proveedor = new Proveedor { ProveedorId = Guid.NewGuid(), Codigo = "ZZTEST-EQUIFAX-" + suffix, Nombre = "ZZTEST Equifax", Tipo = "BURO_CREDITO", Activo = true };
        var empresaProveedor = new EmpresaProveedor { EmpresaProveedorId = Guid.NewGuid(), EmpresaId = company.EmpresaId, ProveedorId = proveedor.ProveedorId, Ambiente = "PRUEBAS", Activo = true };
        var tempRoot = Path.Combine(Path.GetTempPath(), "mapan-test-storage-buro-" + suffix);

        try
        {
            db.AddRange(company, branch, user, membership, product, client, request, proveedor, empresaProveedor);
            await db.SaveChangesAsync();

            var current = new FakeContext(user.UsuarioId, company.EmpresaId, membership.UsuarioEmpresaId);
            var storage = new LocalFileStorage(new FileStorageSettings(tempRoot, 20 * 1024 * 1024, new HashSet<string> { "application/pdf" }));
            var extraction = new TextPdfDocumentExtractionService(db, storage);
            var repository = new DocumentoRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System), storage, extraction, NullLogger<DocumentoRepository>.Instance);

            using var stream = new MemoryStream(DocumentExtractionPatternTests.BuildSampleReporteBuro());
            var uploaded = await repository.UploadAsync(company.EmpresaId, membership.UsuarioEmpresaId, request.SolicitudCreditoId, "Reporte de buró ZZTEST", "REPORTE_BURO", stream, "application/pdf", default);

            // Tres confirmaciones separadas, cada una con su propio ValidateAsync (y por lo tanto su
            // propio SaveChanges) — exactamente como sucede al confirmar campos uno por uno en la UI.
            await repository.ValidateAsync(company.EmpresaId, membership.UsuarioEmpresaId, uploaded.SolicitudDocumentoId,
                new ValidacionInput(null, "reporte_buro_score", "NUMERO", null, 680m, null, null, null, "CONFIRMADO", null), default);
            await repository.ValidateAsync(company.EmpresaId, membership.UsuarioEmpresaId, uploaded.SolicitudDocumentoId,
                new ValidacionInput(null, "reporte_buro_deuda_total", "NUMERO", null, 910.00m, null, null, null, "CONFIRMADO", null), default);
            await repository.ValidateAsync(company.EmpresaId, membership.UsuarioEmpresaId, uploaded.SolicitudDocumentoId,
                new ValidacionInput(null, "reporte_buro_mora_actual_dias", "NUMERO", null, 5m, null, null, null, "CONFIRMADO", null), default);

            var snapshot = await db.Set<BuroSnapshot>().AsNoTracking().SingleAsync(b => b.EmpresaId == company.EmpresaId && b.SolicitudDocumentoId == uploaded.SolicitudDocumentoId);
            Assert.Equal(680m, snapshot.ScoreBuro);
            Assert.Equal(910.00m, snapshot.DeudaTotal);
            Assert.Equal(5, snapshot.MoraActualMaxDias); // era el bug: quedaba null porque la confirmación en curso no era visible para su propia reconstrucción.
            Assert.Equal("CARGA_DOCUMENTO", (await db.Set<ConsultaExterna>().AsNoTracking().SingleAsync(c => c.EmpresaId == company.EmpresaId && c.ConsultaExternaId == snapshot.ConsultaExternaId)).ReferenciaExterna);

            var investigacion = await new InvestigacionRepository(db, new AuditWriter(db, current, current, new HttpContextAccessor(), TimeProvider.System),
                new MockEquifaxProvider(), new MockJudicialProvider(), new MockAvalProvider(), TimeProvider.System, NullLogger<InvestigacionRepository>.Instance).GetUltimaAsync(company.EmpresaId, request.SolicitudCreditoId, default);
            Assert.NotNull(investigacion?.Buro);
            Assert.Equal(680, investigacion!.Buro!.Score);
            Assert.Equal(5, investigacion.Buro.MoraActualMaxDias);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM auditoria.evento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.dato_validado WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.dato_extraido WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM integracion.buro_snapshot WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM integracion.consulta_externa WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM integracion.empresa_proveedor WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM integracion.proveedor WHERE proveedor_id={proveedor.ProveedorId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.solicitud_documento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.documento_version WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM documentos.documento WHERE empresa_id={company.EmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.solicitud_credito WHERE solicitud_credito_id={request.SolicitudCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.producto_credito WHERE producto_credito_id={product.ProductoCreditoId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM credito.cliente WHERE cliente_id={client.ClienteId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.sucursal WHERE sucursal_id={branch.SucursalId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario_empresa WHERE usuario_empresa_id={membership.UsuarioEmpresaId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM seguridad.usuario WHERE usuario_id={user.UsuarioId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM organizacion.empresa WHERE empresa_id={company.EmpresaId}");
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);

            Assert.False(await db.Set<Empresa>().AsNoTracking().AnyAsync(x => x.EmpresaId == company.EmpresaId),
                "La limpieza debe dejar la base exactamente como estaba: sin residuo sintético.");
        }
    }

    // Independent safety net: scans by naming convention (not by the ids this run created) so it
    // also catches leftovers from a previous run that crashed before its own cleanup ran.
    [Fact]
    public async Task NoZzTestResidueRemainsInRealDatabase()
    {
        var connectionString = ConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;
        await using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options);
        Assert.False(await db.Set<Empresa>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<Rol>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<Usuario>().AsNoTracking().AnyAsync(x => x.NombreUsuario.StartsWith("zztest")));
        Assert.False(await db.Set<Sucursal>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<ProductoCredito>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<Cliente>().AsNoTracking().AnyAsync(x => x.NumeroIdentificacion.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<PoliticaCredito>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<SolicitudCredito>().AsNoTracking().AnyAsync(x => x.NumeroSolicitud.StartsWith("ZZTEST-")));
        Assert.False(await db.Set<RutaAprobacion>().AsNoTracking().AnyAsync(x => x.Codigo.StartsWith("ZZTEST-")));
    }

    // No modelo/modelo_version is registered for the synthetic company, so ExecuteAsync must never call this.
    private sealed class NeverCalledRiskModelClient : IRiskModelClient
    {
        public Task<RiskModelResponse> PredictAsync(RiskModelRequest request, CancellationToken ct) =>
            throw new InvalidOperationException("No debía llamarse al modelo ML: no hay riesgo.modelo_version en PRODUCCION para esta empresa sintética.");
    }

    // El paso de aprobación ZZTEST sí se activa en este test (hay ruta predeterminada) — un envío real
    // de correo no debe formar parte de una prueba de integración de base de datos.
    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeContext(Guid usuarioId, Guid empresaId, Guid usuarioEmpresaId) : ICurrentUser, ICurrentTenant
    {
        public Guid UsuarioId { get; } = usuarioId;
        public string NombreUsuario => "zztest";
        public Guid EmpresaId { get; } = empresaId;
        public Guid UsuarioEmpresaId { get; } = usuarioEmpresaId;
    }
}
