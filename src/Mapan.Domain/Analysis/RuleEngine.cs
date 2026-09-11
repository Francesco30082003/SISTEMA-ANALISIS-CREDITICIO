using System.Text.Json;

namespace Mapan.Domain.Analysis;

public sealed record RuleAction(string Accion, string Resultado);

public sealed class RuleEngine
{
    private static readonly HashSet<string> Comparisons = ["=", "!=", ">", ">=", "<", "<="];
    public bool Evaluate(string json, IReadOnlyDictionary<string, JsonElement> fields)
    {
        if (json.Length > 65536) throw new AnalysisConfigurationException("Regla demasiado grande.");
        try {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 24 });
            var budget = 256;
            return EvaluateNode(document.RootElement, fields, ref budget);
        }
        catch (JsonException) { throw new AnalysisConfigurationException("JSON de regla inválido."); }
    }
    private static bool EvaluateNode(JsonElement node, IReadOnlyDictionary<string, JsonElement> fields, ref int budget)
    {
        if (--budget < 0 || node.ValueKind != JsonValueKind.Object) throw new AnalysisConfigurationException("Estructura o tamaño de regla inválido.");
        if (node.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() > 1)) throw new AnalysisConfigurationException("Propiedades duplicadas en regla.");
        var op = String(node, "operador");
        if (op is "AND" or "OR") {
            Only(node, "operador", "condiciones");
            if (!node.TryGetProperty("condiciones", out var children) || children.ValueKind != JsonValueKind.Array || children.GetArrayLength() == 0)
                throw new AnalysisConfigurationException("AND/OR requiere condiciones no vacías.");
            var result = op == "AND";
            foreach (var child in children.EnumerateArray()) {
                // Evaluar todos los nodos para detectar configuración inválida aun sin cortocircuito lógico.
                var evaluated = EvaluateNode(child, fields, ref budget);
                result = op == "AND" ? result & evaluated : result | evaluated;
            }
            return result;
        }
        if (!Comparisons.Contains(op)) throw new AnalysisConfigurationException($"Operador no soportado: {op}.");
        Only(node, "campo", "operador", "valor", "comparar_con");
        var left = Field(fields, String(node, "campo"));
        var hasValue = node.TryGetProperty("valor", out var right);
        var hasField = node.TryGetProperty("comparar_con", out _);
        if (hasValue == hasField) throw new AnalysisConfigurationException("Defina valor o comparar_con, exclusivamente.");
        if (hasField) right = Field(fields, String(node, "comparar_con"));
        if (left.ValueKind == JsonValueKind.Null || right.ValueKind == JsonValueKind.Null)
            throw new AnalysisConfigurationException("No se puede evaluar un campo sin dato confirmado.");
        int comparison;
        if (left.ValueKind == JsonValueKind.Number && right.ValueKind == JsonValueKind.Number
            && left.TryGetDecimal(out var l) && right.TryGetDecimal(out var r)) comparison = l.CompareTo(r);
        else if (left.ValueKind == JsonValueKind.String && right.ValueKind == JsonValueKind.String && op is "=" or "!=")
            comparison = string.CompareOrdinal(left.GetString(), right.GetString());
        else if (left.ValueKind is JsonValueKind.True or JsonValueKind.False && right.ValueKind is JsonValueKind.True or JsonValueKind.False && op is "=" or "!=")
            comparison = left.GetBoolean().CompareTo(right.GetBoolean());
        else throw new AnalysisConfigurationException("Tipos incompatibles o comparación no soportada.");
        return op switch { "=" => comparison == 0, "!=" => comparison != 0, ">" => comparison > 0,
            ">=" => comparison >= 0, "<" => comparison < 0, "<=" => comparison <= 0, _ => false };
    }
    public RuleAction ParseAction(string json)
    {
        if (json.Length > 65536) throw new AnalysisConfigurationException("Acción demasiado grande.");
        try {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth=4 });
            var root=doc.RootElement; Only(root,"accion","resultado");
            if (root.EnumerateObject().GroupBy(p=>p.Name).Any(g=>g.Count()>1)) throw new AnalysisConfigurationException("Acción ambigua.");
            var action=String(root,"accion"); var result=String(root,"resultado");
            if(action!="GENERAR_ALERTA") throw new AnalysisConfigurationException("Acción no soportada.");
            return new(action,result);
        }
        catch(JsonException) { throw new AnalysisConfigurationException("JSON de acción inválido."); }
    }
    private static JsonElement Field(IReadOnlyDictionary<string,JsonElement> fields,string key) =>
        fields.TryGetValue(key,out var value) ? value : throw new AnalysisConfigurationException($"Campo no disponible: {key}.");
    private static string String(JsonElement node,string key) => node.ValueKind==JsonValueKind.Object
        && node.TryGetProperty(key,out var value) && value.ValueKind==JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
        ? value.GetString()! : throw new AnalysisConfigurationException($"Falta {key} en regla.");
    private static void Only(JsonElement node,params string[] allowed)
    {
        if(node.ValueKind!=JsonValueKind.Object || node.EnumerateObject().Any(p=>!allowed.Contains(p.Name)))
            throw new AnalysisConfigurationException("Estructura de regla no soportada.");
    }
}
