using System.Text;
using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Ai.Adapters;

// Named SemanticKernelChunker to keep registration in DI unchanged; implementation
// avoids the SemanticKernel package (vulnerability NU1904) and is functionally equivalent.
internal sealed class SemanticKernelChunker : IChunker
{
    public IReadOnlyList<TextChunk> Chunk(string text, int maxTokens, int overlapTokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var lines = SplitIntoLines(text, maxTokens);
        return BuildChunks(lines, maxTokens, overlapTokens);
    }

    private static List<string> SplitIntoLines(string text, int maxTokens)
    {
        var result = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (Estimate(line) <= maxTokens)
            {
                result.Add(line);
                continue;
            }

            // Long line: break at word boundaries
            var current = new StringBuilder();
            foreach (var word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (Estimate(candidate) > maxTokens && current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0) result.Add(current.ToString());
        }
        return result;
    }

    private static List<TextChunk> BuildChunks(List<string> lines, int maxTokens, int overlapTokens)
    {
        var chunks = new List<TextChunk>();
        var current = new StringBuilder();
        int ordinal = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var prospective = current.Length == 0 ? lines[i] : current + "\n" + lines[i];
            if (Estimate(prospective) > maxTokens && current.Length > 0)
            {
                Emit(chunks, current.ToString(), ref ordinal);

                // overlap: rewind some lines to fill overlapTokens budget
                current.Clear();
                int budget = overlapTokens;
                int start = i;
                while (start > 0 && budget > 0)
                {
                    start--;
                    budget -= Estimate(lines[start]);
                }
                for (int j = start; j < i; j++)
                {
                    if (current.Length > 0) current.Append('\n');
                    current.Append(lines[j]);
                }
            }
            if (current.Length > 0) current.Append('\n');
            current.Append(lines[i]);
        }

        if (current.Length > 0)
            Emit(chunks, current.ToString(), ref ordinal);

        return chunks;
    }

    private static void Emit(List<TextChunk> chunks, string text, ref int ordinal)
    {
        var content = text.Trim();
        if (content.Length > 0)
            chunks.Add(new TextChunk(content, Estimate(content), ordinal++));
    }

    private static int Estimate(string text) => (text.Length + 3) / 4;
}
