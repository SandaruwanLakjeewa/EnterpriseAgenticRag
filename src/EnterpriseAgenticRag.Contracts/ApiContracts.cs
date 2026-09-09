namespace EnterpriseAgenticRag.Contracts;

public sealed record CreateConversationRequest(Guid KnowledgeBaseId, string? Title);
public sealed record ConversationResponse(Guid Id, Guid KnowledgeBaseId, string Title, DateTimeOffset CreatedAt);
public sealed record SendMessageRequest(string Message);
public sealed record CitationResponse(Guid DocumentId, Guid ChunkId, string Title, string SourceUri, string Excerpt);
public sealed record ChatResponse(Guid RunId, Guid MessageId, string Status, string Answer, IReadOnlyList<CitationResponse> Citations);

public sealed record CreateKnowledgeBaseRequest(string Name, string? Description);
public sealed record KnowledgeBaseResponse(Guid Id, string Name, string Description, DateTimeOffset CreatedAt);
public sealed record SubmitTextDocumentRequest(
    string Title,
    string Content,
    string? SourceUri,
    bool IsPublicWithinTenant,
    IReadOnlyList<string>? AllowedGroupIds);
public sealed record SubmitDocumentResponse(Guid DocumentId, Guid JobId, string Status);
public sealed record IngestionJobResponse(Guid Id, Guid DocumentId, string Status, int AttemptCount, string? Error);

public sealed record ApprovalDecisionRequest(string Decision, string? Comment);
public sealed record ApprovalResponse(Guid Id, string Status, DateTimeOffset? DecidedAt);
