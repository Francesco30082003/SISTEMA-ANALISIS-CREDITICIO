namespace Mapan.Domain.Entities;

public sealed class Parametro
{
    public Guid ParametroId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PoliticaVersionId { get; set; }
    public required string Codigo { get; set; }
    public required string Nombre { get; set; }
    public required string TipoDato { get; set; }
    public string? ValorTexto { get; set; }
    public decimal? ValorNumerico { get; set; }
    public bool? ValorBooleano { get; set; }
    public DateOnly? ValorFecha { get; set; }
    public string? ValorJson { get; set; }
    public string? Descripcion { get; set; }
}
