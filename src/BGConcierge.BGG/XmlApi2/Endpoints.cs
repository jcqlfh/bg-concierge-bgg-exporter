using System.Xml;
using Refit;

namespace BGConcierge.BGG.XmlApi2;

public interface Endpoints : IDisposable
{
    [Get("/thing?type=boardgame&stats=1&")]
    Task<XmlDocument> GetThings([AliasAs("id")][Query(CollectionFormat.Csv)] IList<int> listOfIds);
}