namespace EnterpriseAgenticRag.Application;

public sealed record CreateConversationCommand(Guid KnowledgeBaseId, string? Title);
public sealed record ConversationResult(Guid Id, Guid KnowledgeBaseId, string Title, DateTimeOffset CreatedAt);
public sealed record SendMessageCommand(Guid ConversationId, string Message);
public sealed record CitationResult(Guid DocumentId, Guid ChunkId, string Title, string SourceUri, string Excerpt);
public sealed record ChatResult(Guid RunId, Guid MessageId, string Status, string Answer, IReadOnlyList<CitationResult> Citations);
public sealed record CreateKnowledgeBaseCommand(string Name, string? Description);
public sealed record KnowledgeBaseResult(Guid Id, string Name, string Description, DateTimeOffset CreatedAt);
public sealed record SubmitDocumentCommand(
    Guid KnowledgeBaseId,
    string Title,
    string Content,
    string? SourceUri,
    bool IsPublicWithinTenant,
    IReadOnlyList<string> AllowedGroupIds);
public sealed record SubmitDocumentResult(Guid DocumentId, Guid JobId, string Status);
public sealed record IngestionJobResult(Guid Id, Guid DocumentId, string Status, int AttemptCount, string? Error);
