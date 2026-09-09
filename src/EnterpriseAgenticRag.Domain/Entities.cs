namespace EnterpriseAgenticRag.Domain;

public enum MembershipStatus { Active = 1, Suspended = 2 }
public enum DocumentStatus { Pending = 1, Indexed = 2, Failed = 3 }
public enum IngestionJobStatus { Queued = 1, Processing = 2, Completed = 3, Failed = 4 }
public enum AgentRunStatus { Running = 1, Completed = 2, Failed = 3, AwaitingApproval = 4 }
public enum ApprovalStatus { Pending = 1, Approved = 2, Rejected = 3, Expired = 4 }
public enum PrincipalType { User = 1, Group = 2 }

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ApplicationUser
{
    public Guid Id { get; set; }
    public string IdentityProviderSubject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TenantMembership
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}

public sealed class KnowledgeBase
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public string RawContent { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public bool IsPublicWithinTenant { get; set; }
    public DocumentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<DocumentAccessGrant> AccessGrants { get; set; } = [];
    public List<DocumentChunk> Chunks { get; set; } = [];
}

public sealed class DocumentAccessGrant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public PrincipalType PrincipalType { get; set; }
    public string PrincipalId { get; set; } = string.Empty;
}

public sealed class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string SourceUri { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IngestionJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public IngestionJobStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Conversation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ConversationMessage> Messages { get; set; } = [];
}

public sealed class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<MessageCitation> Citations { get; set; } = [];
}

public sealed class MessageCitation
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ChunkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
}

public sealed class AgentRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid InputMessageId { get; set; }
    public Guid? OutputMessageId { get; set; }
    public AgentRunStatus Status { get; set; }
    public string WorkflowName { get; set; } = "agentic-rag-v1";
    public string ModelName { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class AgentSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string FrameworkVersion { get; set; } = string.Empty;
    public string StateJson { get; set; } = "{}";
    public int StateVersion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ApprovalRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RunId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string ArgumentsJson { get; set; } = "{}";
    public ApprovalStatus Status { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AuditEvent
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}
