using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Npgsql;

if (args.Contains("--test-smtp"))
{
    var cfg = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
    var host = cfg["Smtp:Host"]; var port = cfg.GetValue("Smtp:Port", 587);
    var user = cfg["Smtp:User"]; var password = cfg["Smtp:Password"];
    var from = cfg["Smtp:From"] ?? user ?? "";
    using var client = new SmtpClient(host, port) { EnableSsl = cfg.GetValue("Smtp:EnableSsl", true), Credentials = new NetworkCredential(user, password) };
    using var message = new MailMessage(from, user!, "MAPAN - prueba de configuración SMTP", "Si recibes este correo, las credenciales SMTP configuradas en user-secrets funcionan correctamente.");
    await client.SendMailAsync(message);
    Console.WriteLine($"Correo de prueba enviado a {user}.");
    return 0;
}

// Cambios de esquema puntuales sobre la base real: no hay migraciones EF Core en este repo (el esquema
// físico se documenta en docs/PMCORP_MAPAN_MODELO_FISICO_COMPLETO_CODEX.sql y se aplica externamente).
// Cada entrada usa "IF NOT EXISTS" para poder correr esta herramienta más de una vez sin efecto adicional.
var migrations = new (string Name, string Sql)[]
{
    ("credito.cliente: agregar telefono y correo",
     "ALTER TABLE credito.cliente ADD COLUMN IF NOT EXISTS telefono character varying(30); " +
     "ALTER TABLE credito.cliente ADD COLUMN IF NOT EXISTS correo character varying(200);"),
    ("credito.producto_credito: agregar tasa de interes, tipo de tasa y vigencia",
     "ALTER TABLE credito.producto_credito ADD COLUMN IF NOT EXISTS tasa_interes_anual_pct numeric(9,4); " +
     "ALTER TABLE credito.producto_credito ADD COLUMN IF NOT EXISTS tipo_tasa character varying(20) DEFAULT 'FIJA'; " +
     "ALTER TABLE credito.producto_credito ADD COLUMN IF NOT EXISTS vigente_desde date; " +
     "ALTER TABLE credito.producto_credito ADD COLUMN IF NOT EXISTS vigente_hasta date; " +
     "ALTER TABLE credito.producto_credito ALTER COLUMN vigente_desde TYPE date USING vigente_desde::date; " +
     "ALTER TABLE credito.producto_credito ALTER COLUMN vigente_hasta TYPE date USING vigente_hasta::date; " +
     "ALTER TABLE credito.producto_credito DROP CONSTRAINT IF EXISTS ck_producto_tasa; " +
     "ALTER TABLE credito.producto_credito ADD CONSTRAINT ck_producto_tasa CHECK (tasa_interes_anual_pct IS NULL OR tasa_interes_anual_pct >= 0); " +
     "ALTER TABLE credito.producto_credito DROP CONSTRAINT IF EXISTS ck_producto_vigencia; " +
     "ALTER TABLE credito.producto_credito ADD CONSTRAINT ck_producto_vigencia CHECK (vigente_desde IS NULL OR vigente_hasta IS NULL OR vigente_hasta >= vigente_desde);"),
    ("credito.solicitud_credito: agregar desglose de capital, interes y total a pagar estimados",
     "ALTER TABLE credito.solicitud_credito ADD COLUMN IF NOT EXISTS capital_mensual_estimado numeric(18,2); " +
     "ALTER TABLE credito.solicitud_credito ADD COLUMN IF NOT EXISTS interes_mensual_estimado numeric(18,2); " +
     "ALTER TABLE credito.solicitud_credito ADD COLUMN IF NOT EXISTS interes_total_estimado numeric(18,2); " +
     "ALTER TABLE credito.solicitud_credito ADD COLUMN IF NOT EXISTS total_a_pagar_estimado numeric(18,2);"),
    ("politica.regla: agregar etapa (PREEVALUACION/ANALISIS)",
     "ALTER TABLE politica.regla ADD COLUMN IF NOT EXISTS etapa character varying(20) NOT NULL DEFAULT 'ANALISIS'; " +
     "ALTER TABLE politica.regla DROP CONSTRAINT IF EXISTS ck_regla_etapa; " +
     "ALTER TABLE politica.regla ADD CONSTRAINT ck_regla_etapa CHECK (etapa IN ('PREEVALUACION','ANALISIS'));"),
    ("politica.preevaluacion: tabla nueva (historico de resultados de preevaluacion)",
     "CREATE TABLE IF NOT EXISTS politica.preevaluacion (" +
     "preevaluacion_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), " +
     "empresa_id uuid NOT NULL, " +
     "solicitud_credito_id uuid NOT NULL, " +
     "politica_version_id uuid NOT NULL, " +
     "resultado_preliminar character varying(30) NOT NULL, " +
     "severidad_maxima character varying(20), " +
     "detalle_json jsonb, " +
     "ejecutada_por_usuario_empresa_id uuid NOT NULL, " +
     "fecha_ejecucion timestamp with time zone NOT NULL DEFAULT now(), " +
     "CONSTRAINT ck_preevaluacion_resultado CHECK (resultado_preliminar IN ('APTO','REQUIERE_EXCEPCION','NO_APTO')), " +
     "CONSTRAINT fk_preevaluacion_empresa FOREIGN KEY (empresa_id) REFERENCES organizacion.empresa(empresa_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_preevaluacion_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_preevaluacion_politica_version FOREIGN KEY (empresa_id,politica_version_id) REFERENCES politica.politica_version(empresa_id,politica_version_id) ON DELETE RESTRICT" +
     "); " +
     "CREATE INDEX IF NOT EXISTS ix_preevaluacion_solicitud ON politica.preevaluacion(empresa_id,solicitud_credito_id);"),
    ("politica.excepcion: tabla nueva (solicitud/aprobacion de excepciones a politica)",
     "CREATE TABLE IF NOT EXISTS politica.excepcion (" +
     "excepcion_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), " +
     "empresa_id uuid NOT NULL, " +
     "solicitud_credito_id uuid NOT NULL, " +
     "regla_id uuid, " +
     "motivo_justificacion text NOT NULL, " +
     "observacion text, " +
     "evidencia text, " +
     "estado character varying(20) NOT NULL DEFAULT 'SOLICITADA', " +
     "solicitada_por_usuario_empresa_id uuid NOT NULL, " +
     "fecha_solicitud timestamp with time zone NOT NULL DEFAULT now(), " +
     "resuelta_por_usuario_empresa_id uuid, " +
     "fecha_resolucion timestamp with time zone, " +
     "comentario_resolucion text, " +
     "CONSTRAINT ck_excepcion_estado CHECK (estado IN ('SOLICITADA','APROBADA','RECHAZADA')), " +
     "CONSTRAINT fk_excepcion_empresa FOREIGN KEY (empresa_id) REFERENCES organizacion.empresa(empresa_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_excepcion_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_excepcion_regla FOREIGN KEY (empresa_id,regla_id) REFERENCES politica.regla(empresa_id,regla_id) ON DELETE RESTRICT" +
     "); " +
     "CREATE INDEX IF NOT EXISTS ix_excepcion_solicitud ON politica.excepcion(empresa_id,solicitud_credito_id);"),
    ("documentos.documento: agregar estado de verificacion documental",
     "ALTER TABLE documentos.documento ADD COLUMN IF NOT EXISTS estado_verificacion character varying(20) NOT NULL DEFAULT 'PENDIENTE'; " +
     "ALTER TABLE documentos.documento DROP CONSTRAINT IF EXISTS ck_documento_estado_verificacion; " +
     "ALTER TABLE documentos.documento ADD CONSTRAINT ck_documento_estado_verificacion CHECK (estado_verificacion IN ('PENDIENTE','VERIFICADO','SOSPECHOSO','INCONSISTENTE'));"),
    ("documentos.verificacion: tabla nueva (historico de verificacion de autenticidad documental)",
     "CREATE TABLE IF NOT EXISTS documentos.verificacion (" +
     "verificacion_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), " +
     "empresa_id uuid NOT NULL, " +
     "documento_id uuid NOT NULL, " +
     "empresa_proveedor_id uuid, " +
     "resultado character varying(20) NOT NULL, " +
     "confianza_pct numeric(5,2), " +
     "motivos_json jsonb, " +
     "observacion text, " +
     "ejecutada_por_usuario_empresa_id uuid, " +
     "fecha_creacion timestamp with time zone NOT NULL DEFAULT now(), " +
     "CONSTRAINT ck_verificacion_resultado CHECK (resultado IN ('VERIFICADO','SOSPECHOSO','INCONSISTENTE')), " +
     "CONSTRAINT fk_verificacion_documento FOREIGN KEY (empresa_id,documento_id) REFERENCES documentos.documento(empresa_id,documento_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_verificacion_empresa_proveedor FOREIGN KEY (empresa_id,empresa_proveedor_id) REFERENCES integracion.empresa_proveedor(empresa_id,empresa_proveedor_id) ON DELETE RESTRICT" +
     "); " +
     "CREATE INDEX IF NOT EXISTS ix_verificacion_documento ON documentos.verificacion(empresa_id,documento_id,fecha_creacion DESC);"),
    ("integracion.iess_snapshot: tabla nueva (resultado de verificacion laboral IESS)",
     "CREATE TABLE IF NOT EXISTS integracion.iess_snapshot (" +
     "iess_snapshot_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), " +
     "empresa_id uuid NOT NULL, " +
     "solicitud_credito_id uuid NOT NULL, " +
     "empresa_proveedor_id uuid NOT NULL, " +
     "consulta_externa_id uuid, " +
     "relacion_laboral_activa boolean, " +
     "empleador_registrado character varying(200), " +
     "fecha_afiliacion date, " +
     "aporte_mensual numeric(18,2), " +
     "estado character varying(30), " +
     "payload_normalizado jsonb, " +
     "fecha_creacion timestamp with time zone NOT NULL DEFAULT now(), " +
     "CONSTRAINT fk_iess_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_iess_empresa_proveedor FOREIGN KEY (empresa_id,empresa_proveedor_id) REFERENCES integracion.empresa_proveedor(empresa_id,empresa_proveedor_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_iess_consulta FOREIGN KEY (empresa_id,solicitud_credito_id,consulta_externa_id) REFERENCES integracion.consulta_externa(empresa_id,solicitud_credito_id,consulta_externa_id) ON DELETE RESTRICT" +
     "); " +
     "CREATE INDEX IF NOT EXISTS ix_iess_snapshot_solicitud ON integracion.iess_snapshot(empresa_id,solicitud_credito_id,fecha_creacion DESC);"),
    ("credito.producto_credito: agregar categoria (microcredito/comercial/consumo/otro)",
     "ALTER TABLE credito.producto_credito ADD COLUMN IF NOT EXISTS categoria character varying(20) NOT NULL DEFAULT 'OTRO'; " +
     "ALTER TABLE credito.producto_credito DROP CONSTRAINT IF EXISTS ck_producto_categoria; " +
     "ALTER TABLE credito.producto_credito ADD CONSTRAINT ck_producto_categoria CHECK (categoria IN ('MICROCREDITO','COMERCIAL','CONSUMO','OTRO'));"),
    ("credito.visita_negocio: tabla nueva (visita de campo al negocio del cliente)",
     "CREATE TABLE IF NOT EXISTS credito.visita_negocio (" +
     "visita_negocio_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), " +
     "empresa_id uuid NOT NULL, " +
     "solicitud_credito_id uuid NOT NULL, " +
     "fecha_visita date NOT NULL, " +
     "direccion_observada text, " +
     "latitud numeric(9,6), " +
     "longitud numeric(9,6), " +
     "negocio_existe boolean NOT NULL, " +
     "tipo_negocio_observado character varying(200), " +
     "tiempo_funcionamiento_observado character varying(100), " +
     "numero_empleados_observado integer, " +
     "inventario_estimado numeric(18,2), " +
     "ingreso_mensual_estimado_observado numeric(18,2), " +
     "observaciones text, " +
     "recomendacion character varying(30) NOT NULL, " +
     "realizada_por_usuario_empresa_id uuid NOT NULL, " +
     "fecha_creacion timestamp with time zone NOT NULL DEFAULT now(), " +
     "CONSTRAINT ck_visita_negocio_recomendacion CHECK (recomendacion IN ('FAVORABLE','FAVORABLE_CON_OBSERVACIONES','DESFAVORABLE')), " +
     "CONSTRAINT fk_visita_negocio_empresa FOREIGN KEY (empresa_id) REFERENCES organizacion.empresa(empresa_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_visita_negocio_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT, " +
     "CONSTRAINT fk_visita_negocio_usuario FOREIGN KEY (empresa_id,realizada_por_usuario_empresa_id) REFERENCES seguridad.usuario_empresa(empresa_id,usuario_empresa_id) ON DELETE RESTRICT" +
     "); " +
     "CREATE INDEX IF NOT EXISTS ix_visita_negocio_solicitud ON credito.visita_negocio(empresa_id,solicitud_credito_id,fecha_creacion DESC);"),
    ("riesgo.vector_caracteristicas: agregar senales de investigacion/verificacion/visita",
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS score_buro integer; " +
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS mora_actual_max_dias_buro integer; " +
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS tiene_procesos_judiciales boolean; " +
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS gravedad_judicial character varying(10); " +
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS documentos_sospechosos integer; " +
     "ALTER TABLE riesgo.vector_caracteristicas ADD COLUMN IF NOT EXISTS visita_recomendacion character varying(30);"),
};

if (!args.Contains("--apply"))
{
    Console.WriteLine("Vista previa. Se aplicarían estos cambios de esquema:");
    foreach (var m in migrations) Console.WriteLine($"- {m.Name}");
    Console.WriteLine("\nEjecutar con --apply para aplicarlos contra la base configurada en user-secrets (ConnectionStrings:MapanDatabase).");
    return 0;
}

try
{
    var config = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
    var connectionString = config.GetConnectionString("MapanDatabase");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("No se encontró ConnectionStrings:MapanDatabase.");
        return 1;
    }
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    foreach (var m in migrations)
    {
        await using var command = new NpgsqlCommand(m.Sql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Aplicado: {m.Name}");
    }
    Console.WriteLine("Migración de esquema completada.");
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"Migración no aplicada ({e.GetType().Name}): {e.Message}");
    return 1;
}
