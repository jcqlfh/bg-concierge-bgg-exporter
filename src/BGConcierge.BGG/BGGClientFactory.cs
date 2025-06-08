using System.Reflection;
using BGConcierge.BGG.XmlApi;
using Refit;

namespace BGConcierge.BGG;

public class BGGClientFactory
{
    public static Endpoints GetXmlApi()
    {
        var handler = new UrlLoggingAndUnescapingHandler()
        {
            InnerHandler = new HttpClientHandler()
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://boardgamegeek.com/xmlapi")
        };

        return RestService.For<Endpoints>(httpClient,
            new RefitSettings
            {
                ContentSerializer = new XmlContentSerializer(),
                UrlParameterFormatter = new NoEncodingUrlParameterFormatter()

            });
    }
}


public class NoEncodingUrlParameterFormatter : IUrlParameterFormatter
{
    public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
    {
        return value?.ToString(); // Não faz encoding
    }
}

public class UrlLoggingAndUnescapingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Primeiro, substitui %2C por vírgulas na URL
        if (request.RequestUri != null)
        {
            var originalUrl = request.RequestUri.ToString();
            var unescapedUrl = originalUrl.Replace("%2C", ",");
            request.RequestUri = new Uri(unescapedUrl);

            // Agora loga a URL corrigida
            Console.WriteLine($"URL requisitada: {unescapedUrl}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
