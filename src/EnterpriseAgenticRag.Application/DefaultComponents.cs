using System.Text.RegularExpressions;

namespace EnterpriseAgenticRag.Application;

public sealed partial class BoundedQueryPlanner : IQueryPlanner
{
    public Task<IReadOnlyList<string>> PlanAsync(string question, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] queries = QuerySeparator().Split(question)
            .Select(x => x.Trim(' ', '?', '.', ','))
            .Where(x => x.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();
        return Task.FromResult<IReadOnlyList<string>>(queries.Length == 0 ? [question] : queries);
    }

    [GeneratedRegex(@"\s+(?:and|also|then)\s+|[?;]+", RegexOptions.IgnoreCase)]
    private static partial Regex QuerySeparator();
}

public sealed class OverlappingTextChunker(int maximumCharacters = 1200, int overlapCharacters = 150) : IDocumentChunker
{
    public IReadOnlyList<string> Chunk(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];
        if (maximumCharacters < 200 || overlapCharacters < 0 || overlapCharacters >= maximumCharacters)
            throw new InvalidOperationException("Invalid chunking configuration.");

        var chunks = new List<string>();
        int start = 0;
        while (start < content.Length)
        {
            int length = Math.Min(maximumCharacters, content.Length - start);
            int end = start + length;
            if (end < content.Length)
            {
                int boundary = content.LastIndexOfAny(['\n', '.', ' '], end - 1, length);
                if (boundary > start + maximumCharacters / 2) end = boundary + 1;
            }

            chunks.Add(content[start..end].Trim());
            if (end >= content.Length) break;
            start = Math.Max(start + 1, end - overlapCharacters);
        }
        return chunks;
    }
}
