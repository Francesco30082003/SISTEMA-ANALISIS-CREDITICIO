using System.ComponentModel.DataAnnotations;
using Mapan.Application.Common;
using Mapan.Application.Security;

namespace Mapan.Application.Clientes;

public sealed class ClienteService(IClienteRepository repository, ICurrentTenant tenant, IPermissionChecker permissions)
{
    public Task<Page<ClienteDto>> ListAsync(string? search, int page, int size, CancellationToken ct)
    {
        permissions.Require("Clientes:Read"); Paging.Validate(page, size);
        if (search?.Length > 200) throw new ApplicationError(400, "VALIDATION", "Búsqueda demasiado larga.");
        return repository.ListAsync(tenant.EmpresaId, search?.Trim(), page, size, ct);
    }
    public async Task<ClienteDto> GetAsync(Guid id, CancellationToken ct)
    {
        permissions.Require("Clientes:Read");
        return await repository.GetAsync(tenant.EmpresaId, id, ct) ?? throw ApplicationError.NotFound();
    }
    public Task<ClienteDto> CreateAsync(ClienteInput input, CancellationToken ct)
    {
        permissions.Require("Clientes:Create"); Validate(input);
        return repository.CreateAsync(tenant.EmpresaId, input, ct);
    }
    public Task<ClienteDto> UpdateAsync(Guid id, ClienteInput input, CancellationToken ct)
    {
        permissions.Require("Clientes:Update"); Validate(input);
        return repository.UpdateAsync(tenant.EmpresaId, id, input, ct);
    }
    public Task InactivateAsync(Guid id, CancellationToken ct)
    {
        permissions.Require("Clientes:Inactivate"); return repository.InactivateAsync(tenant.EmpresaId, id, ct);
    }
    private static void Validate(ClienteInput input)
    {
        if (!Validator.TryValidateObject(input, new ValidationContext(input), [], true))
            throw new ApplicationError(400, "VALIDATION", "Datos de cliente inválidos.");
    }
}
