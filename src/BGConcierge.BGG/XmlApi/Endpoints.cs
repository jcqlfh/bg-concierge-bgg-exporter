using System.Xml;
using Refit;

namespace BGConcierge.BGG.XmlApi;

public interface Endpoints : IDisposable
{
    [Get("/boardgame/{ids}?stats=1")]
    Task<XmlDocument> GetThings(string ids);
}
