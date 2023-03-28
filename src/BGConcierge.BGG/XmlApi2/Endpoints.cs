using System.Xml;
using Refit;

namespace BGConcierge.BGG.XmlApi2;

public interface Endpoints
{
    [Get("thing?type=boardgame,boardgameexpansion&stats=1&id=")]
    XmlDocument GetThings([Query(CollectionFormat.Multi)] IList<int> listIds);
}