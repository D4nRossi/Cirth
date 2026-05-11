using AngleSharp;
using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Parsers;

internal sealed class HtmlDocumentParser : IDocumentParser
{
    public bool CanHandle(string mimeType) => mimeType == "text/html";

    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        using var reader = new StreamReader(content);
        var html = await reader.ReadToEndAsync(ct);

        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        return doc.Body?.TextContent?.Trim() ?? string.Empty;
    }
}
