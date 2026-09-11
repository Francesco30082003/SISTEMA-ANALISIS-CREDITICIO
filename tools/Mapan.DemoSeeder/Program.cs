using Mapan.DemoSeeder;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

if (!args.Contains("--apply"))
{
    Console.WriteLine("Vista previa (no se escribe nada). Este seeder crearía, si faltan:");
    Console.WriteLine($"- 1 empresa demo ({SeedData.EmpresaCodigo}), {SeedData.Sucursales.Length} sucursales, {SeedData.Productos.Length} productos de crédito.");
    Console.WriteLine($"- Hasta {SeedData.TodosLosPermisos.Length} permisos, {SeedData.Roles.Length} roles, {SeedData.Usuarios.Length} usuarios con contraseña conocida.");
    Console.WriteLine($"- {SeedData.Clientes.Length} clientes ficticios y {SeedData.PlanSolicitudes().Count} solicitudes de crédito en distintos estados (bandeja de aprobación y cartera en mora incluidas).");
    Console.WriteLine();
    Console.WriteLine("Ejecutar con --apply para aplicar los cambios contra la base de datos configurada en user-secrets (ConnectionStrings:MapanDatabase).");
    return 0;
}

try
{
    var config = new ConfigurationBuilder().AddUserSecrets<SeedRunnerMarker>().AddEnvironmentVariables().Build();
    var connectionString = config.GetConnectionString("MapanDatabase");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("No se encontró ConnectionStrings:MapanDatabase en user-secrets ni en variables de entorno.");
        return 1;
    }

    var options = new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql(connectionString).Options;
    await using var db = new MapanDbContext(options);

    var runner = new SeedRunner(db);
    await runner.RunAsync(CancellationToken.None);

    Console.WriteLine("Semilla de datos demo aplicada.");
    Console.WriteLine($"Filas nuevas creadas en esta corrida: {runner.Creados}.");
    Console.WriteLine();
    foreach (var line in runner.Log) Console.WriteLine("- " + line);

    Console.WriteLine();
    Console.WriteLine("Usuarios para iniciar sesión (usuario / contraseña):");
    foreach (var u in SeedData.Usuarios)
        Console.WriteLine($"  {u.NombreUsuario,-22} {u.Password,-22} rol={u.RolCodigo}" + (u.SucursalCodigo is null ? "" : $" sucursal={u.SucursalCodigo}"));

    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine($"El seeder falló ({e.GetType().Name}): {e.Message}");
    return 1;
}

// Ancla para AddUserSecrets<T>: resuelve el mismo UserSecretsId declarado en el .csproj (compartido con Mapan.Api).
internal sealed class SeedRunnerMarker;
