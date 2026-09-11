using Mapan.Domain.Entities;
using Microsoft.AspNetCore.Identity;

Console.Write("Usuario: ");
var nombreUsuario = Console.ReadLine();

if (string.IsNullOrWhiteSpace(nombreUsuario))
{
    Console.WriteLine("Usuario requerido.");
    return;
}

Console.Write("Nueva contraseña: ");

var password = string.Empty;

while (true)
{
    var key = Console.ReadKey(intercept: true);

    if (key.Key == ConsoleKey.Enter)
        break;

    if (key.Key == ConsoleKey.Backspace)
    {
        if (password.Length > 0)
        {
            password = password[..^1];
            Console.Write("\b \b");
        }

        continue;
    }

    password += key.KeyChar;
    Console.Write("*");
}

Console.WriteLine();

if (password.Length < 12)
{
    Console.WriteLine("La contraseña debe tener al menos 12 caracteres.");
    return;
}

var usuario = new Usuario
{
    UsuarioId = Guid.NewGuid(),
    NombreUsuario = nombreUsuario,
    Correo = "temp@local",
    PasswordHash = string.Empty,
    Nombres = "Temporal",
    Apellidos = "Temporal",
    Estado = "ACTIVO",
    FechaCreacion = DateTimeOffset.UtcNow,
    FechaActualizacion = DateTimeOffset.UtcNow
};

var hasher = new PasswordHasher<Usuario>();

var hash = hasher.HashPassword(usuario, password);

Console.WriteLine();
Console.WriteLine("HASH:");
Console.WriteLine(hash);
