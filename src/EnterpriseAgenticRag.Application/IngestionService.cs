using EnterpriseAgenticRag.Domain;

namespace EnterpriseAgenticRag.Application;

public sealed class IngestionService(IKnowledgeRepository repository, IDocumentChunker chunker, IUnitOfWork unitOfWork, IClock clock)
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        IngestionJob? job = await repository.ClaimNextJobAsync(cancellationToken);
        if (job is null) return false;

        try
        {
            KnowledgeDocument document = await repository.GetDocumentForIngestionAsync(job.TenantId, job.DocumentId, cancellationToken)
                ?? throw new InvalidOperationException("The ingestion document no longer exists.");
            IReadOnlyList<string> pieces = chunker.Chunk(document.RawContent);
            var chunks = pieces.Select((content, sequence) => new DocumentChunk
            {
                Id = Guid.NewGuid(), TenantId = document.TenantId, KnowledgeBaseId = document.KnowledgeBaseId,
                DocumentId = document.Id, Title = document.Title, Content = content, Sequence = sequence,
                SourceUri = document.SourceUri, CreatedAt = clock.UtcNow
            }).ToArray();

            await repository.ReplaceChunksAsync(document, chunks, cancellationToken);
            document.Status = DocumentStatus.Indexed;
            document.UpdatedAt = clock.UtcNow;
            job.Status = IngestionJobStatus.Completed;
            job.CompletedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            job.Status = IngestionJobStatus.Failed;
            job.Error = exception.Message.Length <= 2000 ? exception.Message : exception.Message[..2000];
            job.CompletedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        return true;
    }
}
