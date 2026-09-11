using System.Security.Claims;
using Mapan.Api.Security;
using Mapan.Application.Common;
using Mapan.Application.Security;
using Mapan.Domain.Entities;
using Mapan.Infrastructure.Persistence;
using Mapan.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mapan.Application.Tests;

public sealed class SecurityTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static TenantIdentity Identity() => new(UserId, "test", new(Guid.NewGuid(), CompanyId, "TEST", "Test", null), ["configured_role"], ["configured_permission"]);
    private static JwtTokenService Tokens() => new(new("test", "test-api", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))), TimeProvider.System);

    [Fact]
    public void SelectionTokenCannotBeUsedAsTenantContext()
    {
        var http = new DefaultHttpContext { User = new(new ClaimsIdentity([
            new("usuario_id", UserId.ToString()), new("empresa_id", CompanyId.ToString()),
            new("token_use", "select_empresa")], "test")) };
        var current = new CurrentContext(new HttpContextAccessor { HttpContext = http });
        Assert.Throws<ApplicationError>(() => current.EmpresaId);
    }
    [Fact]
    public void TenantTokenCannotSelectAnotherCompany()
    {
        var tokens = Tokens();
        Assert.Throws<ApplicationError>(() => tokens.ValidateSelectionToken(tokens.IssueTenantToken(Identity()).Value));
    }
    [Fact]
    public void SelectionTokenIsSignedAndBoundToUser()
    {
        var tokens = Tokens();
        var token = tokens.IssueSelectionToken(UserId);
        Assert.Equal(UserId, tokens.ValidateSelectionToken(token.Value));
        Assert.Throws<ApplicationError>(() => Tokens().ValidateSelectionToken(token.Value));
    }
    [Fact]
    public async Task SelectingNonMembershipIsForbidden()
    {
        var tokens = Tokens();
        var service = new AuthenticationService(new FakeRepository(), new FakePassword(), tokens, TimeProvider.System);
        var error = await Assert.ThrowsAsync<ApplicationError>(() => service.SelectEmpresaAsync(
            tokens.IssueSelectionToken(UserId).Value, Guid.NewGuid(), default));
        Assert.Equal(403, error.Status);
    }
    [Fact]
    public async Task MultiCompanyLoginRequiresSelection()
    {
        var service = new AuthenticationService(new FakeRepository(), new FakePassword(), Tokens(), TimeProvider.System);
        var result = await service.LoginAsync("test", "test-only", default);
        Assert.Null(result.AccessToken);
        Assert.NotNull(result.SelectionToken);
        Assert.Equal(2, result.Empresas.Count);
    }
    [Fact]
    public void PasswordHasherRejectsWrongPassword()
    {
        var user = FakeRepository.User();
        var passwords = new PasswordService();
        user.PasswordHash = passwords.Hash(user, "test-only-password");
        Assert.True(passwords.Verify(user, "test-only-password"));
        Assert.False(passwords.Verify(user, "wrong"));
    }
    [Fact]
    public void ModelUsesRealPermissionColumnsAndTenantRoleKeys()
    {
        using var db = new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql().Options);
        var permission = db.Model.FindEntityType(typeof(Permiso))!;
        Assert.Null(permission.FindProperty("FechaCreacion"));
        Assert.Equal("seguridad", permission.GetSchema());
        Assert.Equal(2, db.Model.FindEntityType(typeof(UsuarioEmpresaRol))!.FindPrimaryKey()!.Properties.Count);
        Assert.Empty(db.ChangeTracker.Entries());
    }
    private sealed class FakePassword : IPasswordService
    {
        public bool Verify(Usuario user, string password) => password == "test-only";
        public string Hash(Usuario user, string password) => throw new NotSupportedException();
    }
    private sealed class FakeRepository : IAuthenticationRepository
    {
        public static Usuario User() => new() { UsuarioId=UserId, NombreUsuario="test", Correo="test@example.invalid", PasswordHash="unused", Nombres="Test", Apellidos="Test", Estado="ACTIVO" };
        public Task<Usuario?> FindUserAsync(string identifier, CancellationToken ct) => Task.FromResult<Usuario?>(User());
        public Task<Usuario?> FindUserAsync(Guid id, CancellationToken ct) => Task.FromResult<Usuario?>(User());
        public Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid id, CancellationToken ct) => Task.FromResult<IReadOnlyList<MembershipDto>>([
            Identity().Membership, new(Guid.NewGuid(), Guid.NewGuid(), "TEST2", "Test 2", null)]);
        public Task<TenantIdentity?> GetIdentityAsync(Guid id, Guid company, CancellationToken ct) =>
            Task.FromResult<TenantIdentity?>(company == CompanyId ? Identity() : null);
        public Task RecordLoginAsync(Usuario? user, bool successful, CancellationToken ct) => Task.CompletedTask;
        public Task UpdatePasswordHashAsync(Guid usuarioId, string passwordHash, CancellationToken ct) => Task.CompletedTask;
    }
}
