using System.Text.RegularExpressions;
using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Domain;
using EnterpriseAgenticRag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgenticRag.Infrastructure.Retrieval;

internal sealed partial class SqlKnowledgeRetriever(EnterpriseRagDbContext dbContext) : IKnowledgeRetriever
{
    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(RetrievalRequest request, CancellationToken cancellationToken)
    {
        string[] groupIds = request.GroupIds.ToArray();
        List<DocumentChunk> candidates = await dbContext.DocumentChunks.AsNoTracking()
            .Where(chunk => chunk.TenantId == request.TenantId && chunk.KnowledgeBaseId == request.KnowledgeBaseId)
            .Where(chunk => dbContext.Documents.Any(document =>
                document.Id == chunk.DocumentId &&
                (document.IsPublicWithinTenant || document.CreatedByUserId == request.UserId ||
                 document.AccessGrants.Any(permission =>
                     (permission.PrincipalType == PrincipalType.User && permission.PrincipalId == request.UserId.ToString()) ||
                     (permission.PrincipalType == PrincipalType.Group && groupIds.Contains(permission.PrincipalId))))))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        string[] terms = WordPattern().Matches(request.Query.ToLowerInvariant())
            .Select(x => x.Value).Where(x => x.Length > 2).Distinct().Take(12).ToArray();

        return candidates.Select(chunk => new RetrievedChunk(
                chunk.DocumentId, chunk.Id, chunk.Title, chunk.SourceUri, chunk.Content,
                Score(chunk, terms)))
            .Where(x => x.Score > 0 || terms.Length == 0)
            .OrderByDescending(x => x.Score)
            .Take(request.Top)
            .ToArray();
    }

    private static double Score(DocumentChunk chunk, IReadOnlyList<string> terms)
    {
        string title = chunk.Title.ToLowerInvariant();
        string content = chunk.Content.ToLowerInvariant();
        return terms.Sum(term => (title.Contains(term, StringComparison.Ordinal) ? 3d : 0d) +
                                 (content.Contains(term, StringComparison.Ordinal) ? 1d : 0d));
    }

    [GeneratedRegex("[a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex WordPattern();
}
