using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Mapan.Api;

// Development only: no keys are written to disk or shared across local accounts.
internal sealed class DevelopmentKeyRepository : IXmlRepository
{
    private readonly List<XElement> elements = [];
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        lock(elements) return elements.Select(element => new XElement(element)).ToArray();
    }
    public void StoreElement(XElement element, string friendlyName)
    {
        lock(elements) elements.Add(new XElement(element));
    }
}
