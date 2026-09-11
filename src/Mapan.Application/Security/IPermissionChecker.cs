namespace Mapan.Application.Security;

public interface IPermissionChecker
{
    void Require(string operation);
    IReadOnlyList<string> AllowedOperations();
}
