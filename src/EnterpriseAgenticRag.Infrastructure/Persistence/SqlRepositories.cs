using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgenticRag.Infrastructure.Persistence;

internal sealed class SqlRepositories(EnterpriseRagDbContext dbContext) :
    IConversationRepository, IKnowledgeRepository, IAgentRunRepository, IUnitOfWork
{
    public Task AddAsync(Conversation conversation, CancellationToken cancellationToken) =>
        dbContext.Conversations.AddAsync(conversation, cancellationToken).AsTask();

    public Task<Conversation?> GetAsync(Guid tenantId, Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        dbContext.Conversations.Include(x => x.Messages).ThenInclude(x => x.Citations)
            .SingleOrDefaultAsync(x => x.Id == conversationId && x.TenantId == tenantId && x.UserId == userId, cancellationToken);

    public Task AddAsync(AgentRun run, CancellationToken cancellationToken) =>
        dbContext.AgentRuns.AddAsync(run, cancellationToken).AsTask();

    public Task AddKnowledgeBaseAsync(KnowledgeBase knowledgeBase, CancellationToken cancellationToken) =>
        dbContext.KnowledgeBases.AddAsync(knowledgeBase, cancellationToken).AsTask();

    public Task<bool> KnowledgeBaseExistsAsync(Guid tenantId, Guid knowledgeBaseId, CancellationToken cancellationToken) =>
        dbContext.KnowledgeBases.AnyAsync(x => x.Id == knowledgeBaseId && x.TenantId == tenantId, cancellationToken);

    public async Task AddDocumentAsync(KnowledgeDocument document, IngestionJob job, CancellationToken cancellationToken)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
        await dbContext.IngestionJobs.AddAsync(job, cancellationToken);
    }

    public Task<IngestionJob?> GetJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken) =>
        dbContext.IngestionJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId, cancellationToken);

    public async Task<IngestionJob?> ClaimNextJobAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        IngestionJob? job = await dbContext.IngestionJobs
            .Where(x => x.Status == IngestionJobStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null) return null;
        job.Status = IngestionJobStatus.Processing;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.AttemptCount++;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    public Task<KnowledgeDocument?> GetDocumentForIngestionAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken) =>
        dbContext.Documents.Include(x => x.Chunks).SingleOrDefaultAsync(x => x.Id == documentId && x.TenantId == tenantId, cancellationToken);

    public async Task ReplaceChunksAsync(KnowledgeDocument document, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        dbContext.DocumentChunks.RemoveRange(document.Chunks);
        await dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
