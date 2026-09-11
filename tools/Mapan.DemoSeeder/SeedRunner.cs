using Mapan.Domain.Credito;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mapan.DemoSeeder;

public sealed class SeedRunner(MapanDbContext db)
{
    private readonly PasswordHasher<Usuario> hasher = new();
    private readonly Random rng = new(20260908);
    private readonly DateTimeOffset now = DateTimeOffset.UtcNow;

    private Guid empresaId;
    private readonly Dictionary<string, Guid> sucursalIds = [];
    private readonly Dictionary<string, Guid> rolIds = [];
    private readonly Dictionary<string, Guid> productoIds = [];
    private readonly Dictionary<string, Guid> usuarioIds = [];
    private readonly Dictionary<string, Guid> usuarioEmpresaIds = [];
    private readonly Dictionary<string, Guid> clienteIds = [];
    private Guid politicaVersionId;
    private Guid rutaAprobacionId;
    private (Guid PasoId, Guid RolId, string Nombre) pasoJefe;
    private (Guid PasoId, Guid RolId, string Nombre) pasoGerencia;

    public readonly List<string> Log = [];
    public int Creados;

    public async Task RunAsync(CancellationToken ct)
    {
        await EnsureEmpresaAsync(ct);
        await EnsureSucursalesAsync(ct);
        await EnsurePermisosAsync(ct);
        await EnsureRolesAsync(ct);
        await EnsureUsuariosAsync(ct);
        await EnsureProductosAsync(ct);
        await EnsurePoliticaVigenteAsync(ct);
        await EnsureRutaAprobacionAsync(ct);
        await EnsureModeloRiesgoAsync(ct);
        await EnsureProveedoresAsync(ct);
        await EnsureClientesAsync(ct);
        await EnsureSolicitudesAsync(ct);
    }

    private async Task EnsureEmpresaAsync(CancellationToken ct)
    {
        var existing = await db.Set<Empresa>().FirstOrDefaultAsync(e => e.Codigo == SeedData.EmpresaCodigo, ct);
        if (existing is not null) { empresaId = existing.EmpresaId; Log.Add($"Empresa '{SeedData.EmpresaCodigo}' ya existía."); return; }
        var empresa = new Empresa
        {
            Codigo = SeedData.EmpresaCodigo,
            NombreLegal = SeedData.EmpresaNombreLegal,
            PaisCodigo = "EC",
            MonedaCodigo = "USD",
            Estado = "ACTIVA",
        };
        db.Add(empresa);
        await db.SaveChangesAsync(ct);
        empresaId = empresa.EmpresaId;
        Creados++;
        Log.Add($"Empresa '{SeedData.EmpresaCodigo}' creada.");
    }

    private async Task EnsureSucursalesAsync(CancellationToken ct)
    {
        foreach (var s in SeedData.Sucursales)
        {
            var existing = await db.Set<Sucursal>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Codigo == s.Codigo, ct);
            if (existing is not null) { sucursalIds[s.Codigo] = existing.SucursalId; continue; }
            var sucursal = new Sucursal { EmpresaId = empresaId, Codigo = s.Codigo, Nombre = s.Nombre, PaisCodigo = "EC", Provincia = s.Provincia, Ciudad = s.Ciudad, Estado = "ACTIVA" };
            db.Add(sucursal);
            await db.SaveChangesAsync(ct);
            sucursalIds[s.Codigo] = sucursal.SucursalId;
            Creados++;
        }
        Log.Add($"Sucursales listas: {string.Join(", ", SeedData.Sucursales.Select(s => s.Codigo))}.");
    }

    private async Task EnsurePermisosAsync(CancellationToken ct)
    {
        var existentes = await db.Set<Permiso>().Select(p => p.Codigo).ToListAsync(ct);
        var faltantes = SeedData.TodosLosPermisos.Except(existentes).ToList();
        foreach (var codigo in faltantes)
        {
            var nombre = codigo.Replace(":Read", ": consultar").Replace(":Manage", ": administrar").Replace(":Execute", ": ejecutar")
                .Replace(":Create", ": crear").Replace(":Update", ": actualizar").Replace(":Send", ": enviar")
                .Replace(":Inactivate", ": inactivar").Replace(":Upload", ": subir").Replace(":Validate", ": validar");
            db.Add(new Permiso { Codigo = codigo, Nombre = nombre, Descripcion = "MAPAN: operación configurable por los administradores de cada empresa." });
        }
        if (faltantes.Count > 0) { await db.SaveChangesAsync(ct); Creados += faltantes.Count; }
        Log.Add($"Permisos: {faltantes.Count} nuevos, {existentes.Count} ya existían.");
    }

    private async Task EnsureRolesAsync(CancellationToken ct)
    {
        var permisoIdPorCodigo = await db.Set<Permiso>().ToDictionaryAsync(p => p.Codigo, p => p.PermisoId, ct);
        foreach (var r in SeedData.Roles)
        {
            var existing = await db.Set<Rol>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Codigo == r.Codigo, ct);
            Guid rolId;
            if (existing is not null) { rolId = existing.RolId; }
            else
            {
                var rol = new Rol { EmpresaId = empresaId, Codigo = r.Codigo, Nombre = r.Nombre, Estado = "ACTIVO" };
                db.Add(rol);
                await db.SaveChangesAsync(ct);
                rolId = rol.RolId;
                Creados++;
            }
            rolIds[r.Codigo] = rolId;

            var asignados = await db.Set<RolPermiso>().Where(x => x.RolId == rolId).Select(x => x.PermisoId).ToListAsync(ct);
            var faltantes = r.Permisos.Select(c => permisoIdPorCodigo[c]).Except(asignados).ToList();
            foreach (var permisoId in faltantes) db.Add(new RolPermiso { RolId = rolId, PermisoId = permisoId });
            if (faltantes.Count > 0) { await db.SaveChangesAsync(ct); Creados += faltantes.Count; }
        }
        Log.Add($"Roles listos: {string.Join(", ", SeedData.Roles.Select(r => r.Codigo))}.");
    }

    private async Task EnsureUsuariosAsync(CancellationToken ct)
    {
        foreach (var u in SeedData.Usuarios)
        {
            var existingUser = await db.Set<Usuario>().FirstOrDefaultAsync(x => x.NombreUsuario == u.NombreUsuario, ct);
            Guid usuarioId;
            if (existingUser is not null) { usuarioId = existingUser.UsuarioId; }
            else
            {
                var usuario = new Usuario
                {
                    NombreUsuario = u.NombreUsuario,
                    Correo = $"{u.NombreUsuario}@mapan-demo.local",
                    PasswordHash = string.Empty,
                    Nombres = u.Nombres,
                    Apellidos = u.Apellidos,
                    Estado = "ACTIVO",
                };
                usuario.PasswordHash = hasher.HashPassword(usuario, u.Password);
                db.Add(usuario);
                await db.SaveChangesAsync(ct);
                usuarioId = usuario.UsuarioId;
                Creados++;
            }
            usuarioIds[u.NombreUsuario] = usuarioId;

            var existingUe = await db.Set<UsuarioEmpresa>().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId, ct);
            Guid usuarioEmpresaId;
            if (existingUe is not null) { usuarioEmpresaId = existingUe.UsuarioEmpresaId; }
            else
            {
                var ue = new UsuarioEmpresa
                {
                    UsuarioId = usuarioId,
                    EmpresaId = empresaId,
                    SucursalPredeterminadaId = u.SucursalCodigo is null ? null : sucursalIds[u.SucursalCodigo],
                    Estado = "ACTIVO",
                };
                db.Add(ue);
                await db.SaveChangesAsync(ct);
                usuarioEmpresaId = ue.UsuarioEmpresaId;
                Creados++;
            }
            usuarioEmpresaIds[u.NombreUsuario] = usuarioEmpresaId;

            var rolId = rolIds[u.RolCodigo];
            var tieneRol = await db.Set<UsuarioEmpresaRol>().AnyAsync(x => x.UsuarioEmpresaId == usuarioEmpresaId && x.RolId == rolId, ct);
            if (!tieneRol) { db.Add(new UsuarioEmpresaRol { UsuarioEmpresaId = usuarioEmpresaId, RolId = rolId }); await db.SaveChangesAsync(ct); Creados++; }
        }
        Log.Add($"Usuarios listos: {SeedData.Usuarios.Length}.");
    }

    private async Task EnsureProductosAsync(CancellationToken ct)
    {
        foreach (var p in SeedData.Productos)
        {
            var existing = await db.Set<ProductoCredito>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Codigo == p.Codigo, ct);
            if (existing is not null)
            {
                productoIds[p.Codigo] = existing.ProductoCreditoId;
                var cambiado = false;
                if (existing.TasaInteresAnualPct is null) { existing.TasaInteresAnualPct = p.TasaInteresAnualPct; existing.TipoTasa ??= "FIJA"; cambiado = true; }
                if (existing.Categoria == "OTRO" && p.Categoria != "OTRO") { existing.Categoria = p.Categoria; cambiado = true; }
                if (cambiado) { await db.SaveChangesAsync(ct); Creados++; }
                continue;
            }
            var producto = new ProductoCredito
            {
                EmpresaId = empresaId,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                MontoMinimo = p.MontoMinimo,
                MontoMaximo = p.MontoMaximo,
                PlazoMinimoMeses = p.PlazoMinimo,
                PlazoMaximoMeses = p.PlazoMaximo,
                MonedaCodigo = "USD",
                TasaInteresAnualPct = p.TasaInteresAnualPct,
                TipoTasa = "FIJA",
                Categoria = p.Categoria,
                Estado = "ACTIVO",
            };
            db.Add(producto);
            await db.SaveChangesAsync(ct);
            productoIds[p.Codigo] = producto.ProductoCreditoId;
            Creados++;
        }
        Log.Add($"Productos listos: {string.Join(", ", SeedData.Productos.Select(p => p.Codigo))}.");
    }

    private async Task EnsurePoliticaVigenteAsync(CancellationToken ct)
    {
        const string codigo = "POL-DEMO-ESTANDAR";
        var existingPolitica = await db.Set<PoliticaCredito>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Codigo == codigo, ct);
        Guid politicaId;
        if (existingPolitica is not null) { politicaId = existingPolitica.PoliticaCreditoId; }
        else
        {
            var politica = new PoliticaCredito { EmpresaId = empresaId, Codigo = codigo, Nombre = "Política de crédito estándar (demo)", Estado = "ACTIVA" };
            db.Add(politica);
            await db.SaveChangesAsync(ct);
            politicaId = politica.PoliticaCreditoId;
            Creados++;
        }
        var existingVersion = await db.Set<PoliticaVersion>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.PoliticaCreditoId == politicaId && x.NumeroVersion == 1, ct);
        if (existingVersion is not null) { politicaVersionId = existingVersion.PoliticaVersionId; Log.Add("Política vigente ya existía."); }
        else
        {
            var adminUsuarioEmpresaId = usuarioEmpresaIds["demo.admin"];
            var version = new PoliticaVersion
            {
                EmpresaId = empresaId,
                PoliticaCreditoId = politicaId,
                NumeroVersion = 1,
                VigenteDesde = now.AddDays(-90),
                Estado = "VIGENTE",
                CreadaPorUsuarioEmpresaId = adminUsuarioEmpresaId,
                AprobadaPorUsuarioEmpresaId = adminUsuarioEmpresaId,
                FechaAprobacion = now.AddDays(-90),
            };
            db.Add(version);
            await db.SaveChangesAsync(ct);
            politicaVersionId = version.PoliticaVersionId;
            Creados++;
            Log.Add("Política vigente creada.");
        }
        // Sin FACTOR_CAPACIDAD ningún análisis puede ejecutarse (CreditAnalysisRepository lo exige).
        var tieneFactor = await db.Set<Parametro>().AnyAsync(p => p.EmpresaId == empresaId && p.PoliticaVersionId == politicaVersionId && p.Codigo == "FACTOR_CAPACIDAD", ct);
        if (!tieneFactor)
        {
            db.Add(new Parametro { EmpresaId = empresaId, PoliticaVersionId = politicaVersionId, Codigo = "FACTOR_CAPACIDAD", Nombre = "Factor de capacidad de pago", TipoDato = "NUMERO", ValorNumerico = 0.35m, Descripcion = "Porcentaje máximo del ingreso disponible que puede absorber la nueva cuota." });
            await db.SaveChangesAsync(ct);
            Creados++;
        }
        await EnsurePreevaluacionReglasAsync(ct);
    }

    // Fase 4: reglas de preevaluación de ejemplo — usan los campos que expone InvestigacionRepository
    // (score_buro, tiene_procesos_judiciales, gravedad_judicial, tiene_mora_como_garante, etc.). Una
    // cooperativa real las reemplaza/ajusta desde el editor de políticas; estas solo dejan el mecanismo
    // probado de punta a punta con datos de los mocks de la Fase 3.
    private async Task EnsurePreevaluacionReglasAsync(CancellationToken ct)
    {
        var reglas = new (string Codigo, string Nombre, string Descripcion, string Severidad, int Prioridad, object Condicion, string Accion)[]
        {
            ("PRE-JUDICIAL-GRAVE", "Proceso judicial de alto riesgo", "Se detectó un proceso judicial de gravedad alta.", "ROJO", 10,
                new { operador = "AND", condiciones = new object[] { new { campo = "tiene_procesos_judiciales", operador = "=", valor = true }, new { campo = "gravedad_judicial", operador = "=", valor = "ALTA" } } },
                "BLOQUEAR"),
            ("PRE-SIN-BURO", "Cliente sin historial en buró", "El cliente no tiene historial crediticio reportado.", "AMARILLO", 20,
                new { campo = "score_buro_disponible", operador = "=", valor = false },
                "REQUIERE_EXCEPCION"),
            ("PRE-SCORE-BAJO", "Score de buró por debajo del mínimo", "El score de buró está en o por debajo de 700.", "AMARILLO", 30,
                new { operador = "AND", condiciones = new object[] { new { campo = "score_buro_disponible", operador = "=", valor = true }, new { campo = "score_buro", operador = "<=", valor = 700 } } },
                "REQUIERE_EXCEPCION"),
            ("PRE-JUDICIAL-LEVE", "Proceso judicial de riesgo menor", "Hay procesos judiciales, pero de gravedad media o baja.", "AMARILLO", 40,
                new { operador = "AND", condiciones = new object[] { new { campo = "tiene_procesos_judiciales", operador = "=", valor = true }, new { campo = "gravedad_judicial", operador = "!=", valor = "ALTA" } } },
                "ALERTA"),
            ("PRE-AVAL-MORA", "Mora como garante de otra operación", "El cliente figura como garante de una operación con mora.", "AMARILLO", 50,
                new { campo = "tiene_mora_como_garante", operador = "=", valor = true },
                "ALERTA"),
        };
        foreach (var r in reglas)
        {
            if (await db.Set<Regla>().AnyAsync(x => x.EmpresaId == empresaId && x.PoliticaVersionId == politicaVersionId && x.Codigo == r.Codigo, ct)) continue;
            db.Add(new Regla
            {
                EmpresaId = empresaId, PoliticaVersionId = politicaVersionId, Codigo = r.Codigo, Nombre = r.Nombre, Descripcion = r.Descripcion,
                Prioridad = r.Prioridad, Severidad = r.Severidad, Etapa = "PREEVALUACION",
                CondicionJson = System.Text.Json.JsonSerializer.Serialize(r.Condicion), AccionJson = System.Text.Json.JsonSerializer.Serialize(new { accion = r.Accion }),
                Activa = true,
            });
            await db.SaveChangesAsync(ct);
            Creados++;
        }
        Log.Add("Reglas de preevaluación demo listas.");
    }

    private async Task EnsureRutaAprobacionAsync(CancellationToken ct)
    {
        var existingRuta = await db.Set<RutaAprobacion>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.Activa, ct);
        if (existingRuta is not null)
        {
            rutaAprobacionId = existingRuta.RutaAprobacionId;
            var pasos = await db.Set<RutaAprobacionPaso>().Where(x => x.EmpresaId == empresaId && x.RutaAprobacionId == rutaAprobacionId).OrderBy(x => x.Orden).ToListAsync(ct);
            if (pasos.Count >= 2)
            {
                pasoJefe = (pasos[0].RutaAprobacionPasoId, pasos[0].RolId, pasos[0].Nombre);
                pasoGerencia = (pasos[1].RutaAprobacionPasoId, pasos[1].RolId, pasos[1].Nombre);
                Log.Add("Ruta de aprobación activa ya existía; se reutiliza.");
                return;
            }
        }
        const string codigo = "RUTA-DEMO-ESTANDAR";
        var ruta = new RutaAprobacion { EmpresaId = empresaId, Codigo = codigo, Nombre = "Ruta estándar (demo)", Prioridad = 100, EsPredeterminada = true, Activa = true };
        db.Add(ruta);
        await db.SaveChangesAsync(ct);
        rutaAprobacionId = ruta.RutaAprobacionId;
        Creados++;

        var jefeRolId = rolIds["JEFE_AGENCIA"];
        var gerenciaRolId = rolIds["GERENCIA"];
        var paso1 = new RutaAprobacionPaso { EmpresaId = empresaId, RutaAprobacionId = rutaAprobacionId, Orden = 1, Nombre = "Revisión Jefe de Agencia", RolId = jefeRolId, Obligatorio = true, CantidadAprobacionesRequeridas = 1, PermiteAprobar = true, PermiteRechazar = true, PermiteDevolver = true };
        var paso2 = new RutaAprobacionPaso { EmpresaId = empresaId, RutaAprobacionId = rutaAprobacionId, Orden = 2, Nombre = "Aprobación Alta Gerencia", RolId = gerenciaRolId, Obligatorio = true, CantidadAprobacionesRequeridas = 1, PermiteAprobar = true, PermiteRechazar = true, PermiteDevolver = true };
        db.Add(paso1); db.Add(paso2);
        await db.SaveChangesAsync(ct);
        Creados += 2;
        pasoJefe = (paso1.RutaAprobacionPasoId, jefeRolId, paso1.Nombre);
        pasoGerencia = (paso2.RutaAprobacionPasoId, gerenciaRolId, paso2.Nombre);
        Log.Add("Ruta de aprobación demo creada (Jefe de Agencia -> Alta Gerencia).");
    }

    // Registra el modelo propio "en entrenamiento" como ACTIVO/PRODUCCION para que el motor de análisis
    // (CreditAnalysisRepository) intente predecir en cada ejecución. Sin artefacto real todavía, el
    // ArtefactoUri apunta a un marcador temporal — HybridRiskModelClient detecta que ml-service no
    // responde y usa Claude como respaldo, dejando la predicción marcada como CLAUDE_TEMPORAL.
    private async Task EnsureModeloRiesgoAsync(CancellationToken ct)
    {
        const string codigo = "MODELO-RIESGO-DEMO";
        var existingModelo = await db.Set<Modelo>().FirstOrDefaultAsync(m => m.EmpresaId == empresaId && m.Codigo == codigo, ct);
        Guid modeloId;
        if (existingModelo is not null) { modeloId = existingModelo.ModeloId; }
        else
        {
            var modelo = new Modelo { EmpresaId = empresaId, Codigo = codigo, Nombre = "Predicción de incumplimiento (demo)", Objetivo = "Estimar probabilidad de incumplimiento crediticio", Estado = "ACTIVO" };
            db.Add(modelo);
            await db.SaveChangesAsync(ct);
            modeloId = modelo.ModeloId;
            Creados++;
        }
        var existingVersion = await db.Set<ModeloVersion>().FirstOrDefaultAsync(v => v.EmpresaId == empresaId && v.ModeloId == modeloId && v.NumeroVersion == 1, ct);
        if (existingVersion is not null) { Log.Add("Modelo de riesgo demo ya existía (ACTIVO/PRODUCCION)."); return; }
        var featureNames = new[] { "ingreso_mensual", "gastos_mensuales", "cuotas_otras_deudas", "deuda_total_actual", "monto_solicitado", "plazo_meses", "cuota_estimada", "max_dias_mora_historico", "creditos_activos", "antiguedad_actividad_meses", "estabilidad_ingresos_score", "historial_interno_score", "ingreso_disponible", "capacidad_nueva_cuota", "deuda_sobre_ingreso", "cuota_sobre_ingreso", "monto_sobre_ingreso" };
        var version = new ModeloVersion
        {
            EmpresaId = empresaId, ModeloId = modeloId, NumeroVersion = 1, Algoritmo = "PENDIENTE_ENTRENAMIENTO",
            Descripcion = "Sin artefacto propio todavía; las predicciones se respaldan temporalmente con Claude (ver mlStatus=CONFIGURADO_CLAUDE_TEMPORAL en cada análisis) hasta acumular datos suficientes para entrenar el modelo real.",
            ArtefactoUri = "pending://entrenamiento-propio", EsquemaCaracteristicas = System.Text.Json.JsonSerializer.Serialize(featureNames),
            UmbralDecision = 0.5m, Estado = "PRODUCCION",
        };
        db.Add(version);
        await db.SaveChangesAsync(ct);
        Creados++;
        Log.Add("Modelo de riesgo demo creado (ACTIVO/PRODUCCION, respaldado por Claude hasta entrenar el propio).");
    }

    // Activa los proveedores mock de investigación del cliente (Fase 3) para la empresa demo, en
    // ambiente PRUEBAS. Sin esta activación InvestigacionRepository simplemente no consulta esa fuente
    // (comportamiento correcto: una cooperativa que no configura Equifax no debe ver datos fabricados).
    private async Task EnsureProveedoresAsync(CancellationToken ct)
    {
        foreach (var p in SeedData.Proveedores)
        {
            var proveedor = await db.Set<Proveedor>().FirstOrDefaultAsync(x => x.Codigo == p.Codigo, ct);
            if (proveedor is null)
            {
                proveedor = new Proveedor { Codigo = p.Codigo, Nombre = p.Nombre, Tipo = p.Tipo, Descripcion = p.Descripcion, Activo = true };
                db.Add(proveedor);
                await db.SaveChangesAsync(ct);
                Creados++;
            }
            var activo = await db.Set<EmpresaProveedor>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.ProveedorId == proveedor.ProveedorId, ct);
            if (activo is null)
            {
                db.Add(new EmpresaProveedor { EmpresaId = empresaId, ProveedorId = proveedor.ProveedorId, Ambiente = "PRUEBAS", Activo = true });
                await db.SaveChangesAsync(ct);
                Creados++;
            }
        }
        Log.Add($"Proveedores de investigación listos: {string.Join(", ", SeedData.Proveedores.Select(p => p.Codigo))}.");
    }

    private async Task EnsureClientesAsync(CancellationToken ct)
    {
        for (var i = 0; i < SeedData.Clientes.Length; i++)
        {
            var c = SeedData.Clientes[i];
            var tipoId = c.TipoPersona == "NATURAL" ? "CEDULA" : "RUC";
            var slug = (c.Nombres ?? c.RazonSocial ?? "cliente").ToLowerInvariant().Replace(" ", ".");
            var telefono = $"+593 99{100 + i:000} {2000 + i * 7:0000}";
            var correo = $"{slug}{i + 1}@ejemplo-demo.com";
            var existing = await db.Set<Cliente>().FirstOrDefaultAsync(x => x.EmpresaId == empresaId && x.TipoIdentificacion == tipoId && x.NumeroIdentificacion == c.NumeroIdentificacion, ct);
            if (existing is not null)
            {
                clienteIds[c.NumeroIdentificacion] = existing.ClienteId;
                if (existing.Telefono is null) { existing.Telefono = telefono; existing.Correo = correo; await db.SaveChangesAsync(ct); Creados++; }
                continue;
            }
            var cliente = new Cliente
            {
                EmpresaId = empresaId,
                TipoPersona = c.TipoPersona,
                TipoIdentificacion = tipoId,
                NumeroIdentificacion = c.NumeroIdentificacion,
                Nombres = c.Nombres,
                Apellidos = c.Apellidos,
                RazonSocial = c.RazonSocial,
                FechaNacimiento = c.TipoPersona == "NATURAL" ? DateOnly.FromDateTime(now.UtcDateTime).AddYears(-rng.Next(22, 60)) : null,
                Telefono = telefono,
                Correo = correo,
                Estado = "ACTIVO",
            };
            db.Add(cliente);
            await db.SaveChangesAsync(ct);
            clienteIds[c.NumeroIdentificacion] = cliente.ClienteId;
            Creados++;
        }
        Log.Add($"Clientes listos: {SeedData.Clientes.Length}.");
    }

    private async Task EnsureSolicitudesAsync(CancellationToken ct)
    {
        var plan = SeedData.PlanSolicitudes();
        var solicitudesCreadas = 0;
        var prestamosCreados = 0;
        foreach (var item in plan)
        {
            var numeroSolicitud = $"SOL-DEMO-{item.Indice:0000}";
            var yaExistia = await db.Set<SolicitudCredito>().AnyAsync(x => x.EmpresaId == empresaId && x.NumeroSolicitud == numeroSolicitud, ct);
            if (yaExistia) continue;

            var sucursalCodigo = SeedData.Sucursales[item.SucursalIndex].Codigo;
            var analistaUsuario = SeedData.Usuarios[item.AnalistaIndex];
            var analistaUsuarioEmpresaId = usuarioEmpresaIds[analistaUsuario.NombreUsuario];
            var cliente = SeedData.Clientes[item.ClienteIndex];
            var clienteId = clienteIds[cliente.NumeroIdentificacion];
            var producto = SeedData.Productos[item.Indice % SeedData.Productos.Length];
            var productoId = productoIds[producto.Codigo];

            var monto = Math.Round(producto.MontoMinimo + (decimal)rng.NextDouble() * (producto.MontoMaximo - producto.MontoMinimo), 2);
            var plazo = rng.Next(producto.PlazoMinimo, producto.PlazoMaximo + 1);
            var tasa = producto.TasaInteresAnualPct;
            var calculo = CreditoFrancesCalculator.Calculate(monto, plazo, tasa);
            var cuota = calculo.CuotaEstimada;

            var solicitud = new SolicitudCredito
            {
                EmpresaId = empresaId,
                SucursalId = sucursalIds[sucursalCodigo],
                ClienteId = clienteId,
                ProductoCreditoId = productoId,
                NumeroSolicitud = numeroSolicitud,
                MontoSolicitado = monto,
                PlazoSolicitadoMeses = plazo,
                TasaInteresAnualPct = tasa,
                CapitalMensualEstimado = calculo.CapitalMensual,
                InteresMensualEstimado = calculo.InteresMensual,
                CuotaEstimada = cuota,
                InteresTotalEstimado = calculo.InteresTotal,
                TotalAPagarEstimado = calculo.TotalAPagar,
                DestinoCredito = $"Capital de trabajo ({producto.Nombre.ToLowerInvariant()}) para actividad {cliente.TipoActividad.ToLowerInvariant()}.",
                Estado = item.EstadoObjetivo,
                CreadoPorUsuarioEmpresaId = analistaUsuarioEmpresaId,
                FechaCreacion = now.AddDays(-rng.Next(5, 120)),
            };
            if (item.EstadoObjetivo is not "BORRADOR") solicitud.FechaEnvio = solicitud.FechaCreacion.AddDays(1);
            if (item.EstadoObjetivo is "APROBADA" or "RECHAZADA") solicitud.FechaFinalizacion = solicitud.FechaCreacion.AddDays(10);
            db.Add(solicitud);
            await db.SaveChangesAsync(ct);
            solicitudesCreadas++;

            db.Add(new ActividadEconomica
            {
                EmpresaId = empresaId,
                SolicitudCreditoId = solicitud.SolicitudCreditoId,
                TipoActividad = cliente.TipoActividad,
                EmpleadorNegocio = cliente.EmpleadorNegocio ?? (cliente.RazonSocial ?? $"{cliente.Nombres} {cliente.Apellidos}"),
                CargoActividad = cliente.TipoPersona == "NATURAL" ? "Propietario" : "Representante legal",
                FechaInicio = DateOnly.FromDateTime(now.UtcDateTime).AddYears(-rng.Next(1, 15)),
                Ruc = cliente.TipoPersona == "JURIDICA" ? $"{cliente.NumeroIdentificacion}001" : null,
                EsPrincipal = true,
                Verificada = item.EstadoObjetivo is not ("BORRADOR" or "DOCUMENTACION"),
            });
            await db.SaveChangesAsync(ct);

            var actividadId = await db.Set<ActividadEconomica>().Where(a => a.EmpresaId == empresaId && a.SolicitudCreditoId == solicitud.SolicitudCreditoId).Select(a => a.ActividadEconomicaId).SingleAsync(ct);
            var tipoIngreso = cliente.TipoPersona == "NATURAL" ? "NEGOCIO_PROPIO" : "VENTAS_NEGOCIO";
            db.Add(new FuenteIngreso
            {
                EmpresaId = empresaId,
                SolicitudCreditoId = solicitud.SolicitudCreditoId,
                ActividadEconomicaId = actividadId,
                TipoIngreso = tipoIngreso,
                Descripcion = $"Ingresos por {cliente.TipoActividad.ToLowerInvariant()}",
                MonedaCodigo = "USD",
                EsRecurrente = true,
                Declarado = true,
                Verificado = item.EstadoObjetivo is not ("BORRADOR" or "DOCUMENTACION"),
            });
            await db.SaveChangesAsync(ct);

            if (item.EstadoObjetivo is "ANALISIS" or "REVISION" or "APROBADA" or "RECHAZADA")
                await SeedAnalisisYFlujoAsync(item, solicitud, analistaUsuarioEmpresaId, monto, plazo, tasa, cuota, ct);

            if (item.EstadoObjetivo == "APROBADA")
            {
                prestamosCreados++;
                await SeedPrestamoAsync(item, solicitud, monto, plazo, tasa, cuota, ct);
            }
        }
        Log.Add($"Solicitudes creadas en esta corrida: {solicitudesCreadas} (de {plan.Count} planificadas). Préstamos creados: {prestamosCreados}.");
    }

    private async Task SeedAnalisisYFlujoAsync(SolicitudPlan item, SolicitudCredito solicitud, Guid analistaUsuarioEmpresaId, decimal monto, int plazo, decimal tasa, decimal cuota, CancellationToken ct)
    {
        var completado = item.EstadoObjetivo is "REVISION" or "APROBADA" or "RECHAZADA";
        var analisis = new Analisis
        {
            EmpresaId = empresaId,
            SolicitudCreditoId = solicitud.SolicitudCreditoId,
            PoliticaVersionId = politicaVersionId,
            NumeroEjecucion = 1,
            TipoAnalisis = "REGLAS",
            Estado = completado ? "COMPLETADO" : "INICIADO",
            EjecutadoPorUsuarioEmpresaId = analistaUsuarioEmpresaId,
            IniciadoEn = solicitud.FechaCreacion.AddDays(2),
            FinalizadoEn = completado ? solicitud.FechaCreacion.AddDays(3) : null,
        };
        db.Add(analisis);
        await db.SaveChangesAsync(ct);
        if (!completado) return;

        var codigoRecomendacion = item.EstadoObjetivo switch
        {
            "RECHAZADA" => "CAPACIDAD_NO_COMPATIBLE",
            "APROBADA" => "CAPACIDAD_COMPATIBLE",
            _ => "REVISION_ADICIONAL",
        };
        var recomendacion = new Recomendacion
        {
            EmpresaId = empresaId,
            AnalisisId = analisis.AnalisisId,
            CodigoRecomendacion = codigoRecomendacion,
            NivelRiesgoFinal = item.EstadoObjetivo == "RECHAZADA" ? "ALTO" : "MEDIO",
            CumpleCapacidad = item.EstadoObjetivo == "APROBADA",
            RequiereRevisionHumana = true,
            Resumen = $"Cuota estimada {cuota:F2} sobre monto {monto:F2} a {plazo} meses ({tasa}% anual).",
            FechaGeneracion = solicitud.FechaCreacion.AddDays(3),
        };
        db.Add(recomendacion);
        await db.SaveChangesAsync(ct);

        var jefeUsuario = SeedData.JefeUsuarioPorSucursal[item.SucursalIndex];
        var jefeUsuarioEmpresaId = usuarioEmpresaIds[jefeUsuario];
        var gerenteUsuario = SeedData.GerentesUsuarios[item.Indice % SeedData.GerentesUsuarios.Length];
        var gerenteUsuarioEmpresaId = usuarioEmpresaIds[gerenteUsuario];

        var aprobacionEstado = item.EtapaAprobacion switch
        {
            "JEFE_PENDIENTE" or "GERENCIA_PENDIENTE" => "EN_CURSO",
            "APROBADA" => "APROBADA",
            "RECHAZADA" => "RECHAZADA",
            _ => "PENDIENTE",
        };
        var aprobacion = new Aprobacion
        {
            EmpresaId = empresaId,
            RecomendacionId = recomendacion.RecomendacionId,
            RutaAprobacionId = rutaAprobacionId,
            Estado = aprobacionEstado,
            CreadaPorUsuarioEmpresaId = analistaUsuarioEmpresaId,
            FechaInicio = solicitud.FechaCreacion.AddDays(3),
            FechaFin = aprobacionEstado is "APROBADA" or "RECHAZADA" ? solicitud.FechaCreacion.AddDays(9) : null,
        };
        db.Add(aprobacion);
        await db.SaveChangesAsync(ct);

        var paso1 = new AprobacionPaso
        {
            EmpresaId = empresaId,
            AprobacionId = aprobacion.AprobacionId,
            RutaAprobacionPasoId = pasoJefe.PasoId,
            Orden = 1,
            NombrePaso = pasoJefe.Nombre,
            RolId = pasoJefe.RolId,
            CantidadAprobacionesRequeridas = 1,
            Estado = item.EtapaAprobacion switch { "JEFE_PENDIENTE" => "ACTIVO", "RECHAZADA" => "RECHAZADO", _ => "APROBADO" },
            FechaHabilitacion = solicitud.FechaCreacion.AddDays(3),
            FechaCompletado = item.EtapaAprobacion == "JEFE_PENDIENTE" ? null : solicitud.FechaCreacion.AddDays(5),
        };
        db.Add(paso1);
        await db.SaveChangesAsync(ct);
        if (paso1.Estado is "APROBADO" or "RECHAZADO")
        {
            db.Add(new DecisionPaso
            {
                EmpresaId = empresaId,
                AprobacionPasoId = paso1.AprobacionPasoId,
                UsuarioEmpresaId = jefeUsuarioEmpresaId,
                Decision = paso1.Estado == "APROBADO" ? "APROBAR" : "RECHAZAR",
                Comentario = paso1.Estado == "APROBADO" ? "Documentación completa, capacidad de pago validada en agencia." : "No cumple política de endeudamiento máximo.",
                FechaDecision = solicitud.FechaCreacion.AddDays(5),
            });
            await db.SaveChangesAsync(ct);
        }

        var paso2Estado = item.EtapaAprobacion switch
        {
            "JEFE_PENDIENTE" => "PENDIENTE",
            "GERENCIA_PENDIENTE" => "ACTIVO",
            "RECHAZADA" => "OMITIDO",
            _ => "APROBADO",
        };
        var paso2 = new AprobacionPaso
        {
            EmpresaId = empresaId,
            AprobacionId = aprobacion.AprobacionId,
            RutaAprobacionPasoId = pasoGerencia.PasoId,
            Orden = 2,
            NombrePaso = pasoGerencia.Nombre,
            RolId = pasoGerencia.RolId,
            CantidadAprobacionesRequeridas = 1,
            Estado = paso2Estado,
            FechaHabilitacion = paso2Estado is "PENDIENTE" or "OMITIDO" ? null : solicitud.FechaCreacion.AddDays(6),
            FechaCompletado = paso2Estado == "APROBADO" ? solicitud.FechaCreacion.AddDays(9) : null,
        };
        db.Add(paso2);
        await db.SaveChangesAsync(ct);
        if (paso2.Estado == "APROBADO")
        {
            db.Add(new DecisionPaso
            {
                EmpresaId = empresaId,
                AprobacionPasoId = paso2.AprobacionPasoId,
                UsuarioEmpresaId = gerenteUsuarioEmpresaId,
                Decision = "APROBAR",
                Comentario = "Aprobado en comité de alta gerencia.",
                FechaDecision = solicitud.FechaCreacion.AddDays(9),
            });
            await db.SaveChangesAsync(ct);
        }
    }

    private static readonly Dictionary<string, int[]> ProgresionMoraPorBanda = new()
    {
        ["15"] = [0, 20],
        ["30"] = [0, 15, 40],
        ["60"] = [0, 20, 45, 70],
        ["90"] = [0, 20, 50, 80, 95],
    };

    private async Task SeedPrestamoAsync(SolicitudPlan item, SolicitudCredito solicitud, decimal monto, int plazo, decimal tasa, decimal cuota, CancellationToken ct)
    {
        var numeroPrestamo = $"PRE-DEMO-{item.Indice:0000}";
        var fechaDesembolso = DateOnly.FromDateTime(solicitud.FechaCreacion.UtcDateTime).AddDays(10);
        var prestamo = new Prestamo
        {
            EmpresaId = empresaId,
            SolicitudCreditoId = solicitud.SolicitudCreditoId,
            NumeroPrestamo = numeroPrestamo,
            MontoDesembolsado = monto,
            FechaDesembolso = fechaDesembolso,
            PlazoMeses = plazo,
            TasaInteresAnualPct = tasa,
            CuotaPactada = cuota,
            FechaVencimiento = fechaDesembolso.AddMonths(plazo),
            Estado = "VIGENTE",
        };
        db.Add(prestamo);
        await db.SaveChangesAsync(ct);
        Creados++;

        var progresion = item.BandaMora is not null ? ProgresionMoraPorBanda[item.BandaMora] : [0];
        var saldo = monto;
        var fechaCorte = fechaDesembolso.AddMonths(1);
        foreach (var diasMora in progresion)
        {
            var pago = diasMora == 0 ? cuota : 0m;
            saldo = Math.Max(0, saldo - Math.Round(cuota * 0.6m, 2));
            db.Add(new DesempenoCredito
            {
                EmpresaId = empresaId,
                PrestamoId = prestamo.PrestamoId,
                FechaCorte = fechaCorte,
                SaldoCapital = saldo,
                CuotaExigible = cuota,
                MontoPagadoPeriodo = pago,
                DiasMora = diasMora,
                EstadoCartera = diasMora == 0 ? "AL_DIA" : "EN_MORA",
            });
            fechaCorte = fechaCorte.AddMonths(1);
        }
        await db.SaveChangesAsync(ct);
        Creados += progresion.Length;
    }
}
