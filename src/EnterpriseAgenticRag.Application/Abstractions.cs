using EnterpriseAgenticRag.Domain;

namespace EnterpriseAgenticRag.Application;

public sealed record RequestIdentity(Guid TenantId, Guid UserId, IReadOnlySet<string> GroupIds);

public interface ICurrentRequestIdentity
{
    RequestIdentity GetRequired();
}

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
    Task<Conversation?> GetAsync(Guid tenantId, Guid userId, Guid conversationId, CancellationToken cancellationToken);
}

public interface IKnowledgeRepository
{
    Task AddKnowledgeBaseAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken);
    Task<bool> KnowledgeBaseExistsAsync(Guid tenantId, Guid knowledgeBaseId, CancellationToken cancellationToken);
    Task AddDocumentAsync(KnowledgeDocument document, IngestionJob job, CancellationToken cancellationToken);
    Task<IngestionJob?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken);
    Task<IngestionJob?> ClaimNextJobAsync(CancellationToken cancellationToken);
    Task<KnowledgeDocument?> GetDocumentForIngestionAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task ReplaceChunksAsync(KnowledgeDocument document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken);
}

public interface IAgentRunRepository
{
    Task AddAsync(AgentRun run, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record RetrievalRequest(Guid TenantId, Guid UserId, IReadOnlySet<string> GroupIds, Guid KnowledgeBaseId, string Query, int Top = 6);
public sealed record RetrievedChunk(Guid DocumentId, Guid ChunkId, string Title, string SourceUri, string Content, double Score);

public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(RetrievalRequest request, CancellationToken cancellationToken);
}

public interface IQueryPlanner
{
    Task<IReadOnlyList<string>> PlanAsync(string question, CancellationToken cancellationToken);
}

public sealed record AnswerRequest(string Question, IReadOnlyList<ConversationMessage> History, IReadOnlyList<RetrievedChunk> Evidence);
public interface IAnswerGenerator
{
    Task<string> GenerateAsync(AnswerRequest request, CancellationToken cancellationToken);
}

public interface IDocumentChunker
{
    IReadOnlyList<string> Chunk(string content);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
