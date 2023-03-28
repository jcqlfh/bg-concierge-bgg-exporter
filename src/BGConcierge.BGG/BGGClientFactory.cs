using BGConcierge.BGG.XmlApi2;
using Refit;

namespace BGConcierge.BGG;

public class BGGClientFactory
{
    public static Endpoints GetXmlApi2()
    {
        return RestService.For<Endpoints>("https://boardgamegeek.com/xmlapi2",
            new RefitSettings {
                ContentSerializer = new XmlContentSerializer()
            });
    }
}