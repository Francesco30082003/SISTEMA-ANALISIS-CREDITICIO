using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Mapan.Infrastructure.Security;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> hasher = new();
    public string Hash(Usuario user, string password) => hasher.HashPassword(user, password);
    public bool Verify(Usuario user, string password)
    {
        try { return hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed; }
        catch (FormatException) { return false; }
    }
}
