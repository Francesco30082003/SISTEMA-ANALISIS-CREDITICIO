using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
var connectionString = configuration.GetConnectionString("MapanDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Falta ConnectionStrings:MapanDatabase en User Secrets o entorno.");
    return 1;
}

try
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
        await readOnly.ExecuteNonQueryAsync();

    const string sql = """
        SELECT n.nspname AS schema, c.relname AS table_name,
          (SELECT jsonb_agg(jsonb_build_object(
              'name', a.attname, 'type', format_type(a.atttypid, a.atttypmod),
              'notNull', a.attnotnull, 'default', pg_get_expr(d.adbin,d.adrelid),
              'generated', a.attgenerated, 'identity', a.attidentity) ORDER BY a.attnum)
           FROM pg_attribute a LEFT JOIN pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
           WHERE a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped) AS columns,
          (SELECT jsonb_agg(jsonb_build_object('name', con.conname, 'type', con.contype,
              'definition', pg_get_constraintdef(con.oid)) ORDER BY con.conname)
           FROM pg_constraint con WHERE con.conrelid=c.oid) AS constraints,
          (SELECT jsonb_agg(jsonb_build_object('name', i.indexname, 'definition', i.indexdef) ORDER BY i.indexname)
           FROM pg_indexes i WHERE i.schemaname=n.nspname AND i.tablename=c.relname) AS indexes
        FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE c.relkind IN ('r','p') AND n.nspname IN
          ('organizacion','seguridad','credito','politica','riesgo','flujo','documentos','integracion','auditoria')
        ORDER BY n.nspname,c.relname
        """;
    await using var command = new NpgsqlCommand(sql, connection, transaction);
    await using var reader = await command.ExecuteReaderAsync();
    var tables = new List<object>();
    while (await reader.ReadAsync())
        tables.Add(new {
            Schema = reader.GetString(0), Table = reader.GetString(1),
            Columns = JsonSerializer.Deserialize<JsonElement>(reader.GetString(2)),
            Constraints = reader.IsDBNull(3) ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(reader.GetString(3)),
            Indexes = reader.IsDBNull(4) ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(reader.GetString(4))
        });
    await reader.CloseAsync();
    await using var permissionsCommand = new NpgsqlCommand("SELECT codigo FROM seguridad.permiso ORDER BY codigo", connection, transaction);
    await using var permissionsReader = await permissionsCommand.ExecuteReaderAsync();
    var permissionCodes = new List<string>();
    while (await permissionsReader.ReadAsync()) permissionCodes.Add(permissionsReader.GetString(0));
    await permissionsReader.CloseAsync();
    await using var businessCommand = new NpgsqlCommand("""
        SELECT jsonb_build_object(
          'policies', (SELECT count(*) FROM politica.politica_credito),
          'versions', (SELECT count(*) FROM politica.politica_version),
          'parameter_codes', (SELECT jsonb_agg(DISTINCT codigo) FROM politica.parametro),
          'rules', (SELECT count(*) FROM politica.regla),
          'configured_rules', (SELECT jsonb_agg(jsonb_build_object('codigo',codigo,'condicion',condicion_json,'accion',accion_json,'activa',activa)) FROM politica.regla),
          'approval_routes', (SELECT count(*) FROM flujo.ruta_aprobacion),
          'model_versions', (SELECT count(*) FROM riesgo.modelo_version))::text
        """, connection, transaction);
    var businessSummary = (string?)await businessCommand.ExecuteScalarAsync();
    await transaction.RollbackAsync();
    var output = args.Length > 0 ? args[0] : "docs/database/actual-schema.json";
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "permission-codes.json"), JsonSerializer.Serialize(permissionCodes));
    await File.WriteAllTextAsync(output, JsonSerializer.Serialize(tables, new JsonSerializerOptions { WriteIndented = true }));
    await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "business-configuration-summary.json"), businessSummary ?? "{}");
    Console.WriteLine($"Inspección de solo lectura: {tables.Count} tablas. Metadatos guardados en {output}.");
    return 0;
}
catch (Exception exception)
{
    // No imprimir mensajes del proveedor: pueden incluir detalles de conexión.
    Console.Error.WriteLine($"No fue posible inspeccionar PostgreSQL ({exception.GetType().Name}). No se modificó la base.");
    return 2;
}
