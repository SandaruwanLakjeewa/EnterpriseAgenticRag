using System.Security.Cryptography;
using System.Text;
using EnterpriseAgenticRag.Domain;

namespace EnterpriseAgenticRag.Application;

public sealed class KnowledgeService(
    ICurrentRequestIdentity currentIdentity,
    IKnowledgeRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<KnowledgeBaseResult> CreateKnowledgeBaseAsync(CreateKnowledgeBaseCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("Name is required.");
        RequestIdentity identity = currentIdentity.GetRequired();
        var knowledgeBase = new KnowledgeBase
        {
            Id = Guid.NewGuid(), TenantId = identity.TenantId, Name = command.Name.Trim(),
            Description = command.Description?.Trim() ?? string.Empty, CreatedAt = clock.UtcNow
        };
        await repository.AddKnowledgeBaseAsync(knowledgeBase, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(knowledgeBase.Id, knowledgeBase.Name, knowledgeBase.Description, knowledgeBase.CreatedAt);
    }

    public async Task<SubmitDocumentResult> SubmitDocumentAsync(SubmitDocumentCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || string.IsNullOrWhiteSpace(command.Content))
            throw new ArgumentException("Document title and content are required.");

        RequestIdentity identity = currentIdentity.GetRequired();
        if (!await repository.KnowledgeBaseExistsAsync(identity.TenantId, command.KnowledgeBaseId, cancellationToken))
            throw new KeyNotFoundException("Knowledge base was not found.");

        Guid documentId = Guid.NewGuid();
        var document = new KnowledgeDocument
        {
            Id = documentId, TenantId = identity.TenantId, KnowledgeBaseId = command.KnowledgeBaseId,
            CreatedByUserId = identity.UserId, Title = command.Title.Trim(), RawContent = command.Content.Trim(),
            SourceUri = string.IsNullOrWhiteSpace(command.SourceUri) ? $"document://{documentId}" : command.SourceUri.Trim(),
            ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Content))),
            IsPublicWithinTenant = command.IsPublicWithinTenant, Status = DocumentStatus.Pending,
            CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow,
            AccessGrants = command.AllowedGroupIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(groupId => new DocumentAccessGrant
            {
                Id = Guid.NewGuid(), TenantId = identity.TenantId, DocumentId = documentId,
                PrincipalType = PrincipalType.Group, PrincipalId = groupId
            }).ToList()
        };
        var job = new IngestionJob
        {
            Id = Guid.NewGuid(), TenantId = identity.TenantId, DocumentId = document.Id,
            Status = IngestionJobStatus.Queued, CreatedAt = clock.UtcNow
        };
        await repository.AddDocumentAsync(document, job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(document.Id, job.Id, "queued");
    }

    public async Task<IngestionJobResult> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        RequestIdentity identity = currentIdentity.GetRequired();
        IngestionJob job = await repository.GetJobAsync(identity.TenantId, jobId, cancellationToken)
            ?? throw new KeyNotFoundException("Ingestion job was not found.");
        return new(job.Id, job.DocumentId, job.Status.ToString().ToLowerInvariant(), job.AttemptCount, job.Error);
    }
}
