namespace Mapan.Application.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public static class Paging
{
    public static void Validate(int page, int size)
    {
        if (page < 1 || size is < 1 or > 100 || page > int.MaxValue / size)
            throw new ApplicationError(400, "VALIDATION", "Página positiva y tamaño de página entre 1 y 100 requeridos.");
    }
}
