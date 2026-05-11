using Cirth.Application.Common.Ports;
using System.Text;
using UglyToad.PdfPig;

namespace Cirth.Infrastructure.Parsers;

internal sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanHandle(string mimeType) => mimeType == "application/pdf";

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(content);
        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }
        return Task.FromResult(sb.ToString());
    }
}
