using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Parsers;

internal sealed class PlainTextDocumentParser : IDocumentParser
{
    public bool CanHandle(string mimeType) => mimeType == "text/plain";

    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        using var reader = new StreamReader(content);
        return await reader.ReadToEndAsync(ct);
    }
}
