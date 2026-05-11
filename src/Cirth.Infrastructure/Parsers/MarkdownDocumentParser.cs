using Cirth.Application.Common.Ports;
using Markdig;
using System.Text.RegularExpressions;

namespace Cirth.Infrastructure.Parsers;

internal sealed class MarkdownDocumentParser : IDocumentParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions().Build();

    public bool CanHandle(string mimeType)
        => mimeType is "text/markdown" or "text/x-markdown";

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct)
    {
        using var reader = new StreamReader(content);
        var markdown = reader.ReadToEnd();
        // Convert to HTML then strip tags — preserves structure better than raw markdown
        var html = Markdown.ToHtml(markdown, Pipeline);
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return Task.FromResult(text);
    }
}
