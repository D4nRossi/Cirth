using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Parsers;

public sealed class CompositeDocumentParser(IEnumerable<IDocumentParser> parsers) : IDocumentParser
{
    public bool CanHandle(string mimeType) => parsers.Any(p => p.CanHandle(mimeType));

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        throw new NotSupportedException("Use IDocumentParserFactory to resolve by mime type.");
    }

    public IDocumentParser? GetParser(string mimeType)
        => parsers.FirstOrDefault(p => p.CanHandle(mimeType));
}
