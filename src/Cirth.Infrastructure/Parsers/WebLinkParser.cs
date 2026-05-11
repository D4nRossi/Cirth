using AngleSharp;
using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Parsers;

internal sealed class WebLinkParser(IHttpClientFactory httpClientFactory) : IDocumentParser
{
    public bool CanHandle(string mimeType) => mimeType == "text/uri-list" || mimeType == "text/plain";

    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        using var reader = new StreamReader(content);
        var url = (await reader.ReadToEndAsync(ct)).Trim();

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url; // Plain text fallback

        using var client = httpClientFactory.CreateClient("web-fetch");
        var html = await client.GetStringAsync(url, ct);

        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        return doc.Body?.TextContent?.Trim() ?? string.Empty;
    }
}
