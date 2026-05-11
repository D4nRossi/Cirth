using Cirth.Application.Common.Ports;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace Cirth.Infrastructure.Parsers;

internal sealed class DocxDocumentParser : IDocumentParser
{
    public bool CanHandle(string mimeType)
        => mimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(content, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return Task.FromResult(string.Empty);

        foreach (var para in body.Elements<Paragraph>())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(para.InnerText);
        }
        return Task.FromResult(sb.ToString());
    }
}
