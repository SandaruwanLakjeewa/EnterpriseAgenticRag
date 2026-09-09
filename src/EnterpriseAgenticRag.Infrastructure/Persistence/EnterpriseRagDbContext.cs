using EnterpriseAgenticRag.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgenticRag.Infrastructure.Persistence;

public sealed class EnterpriseRagDbContext(DbContextOptions<EnterpriseRagDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<KnowledgeDocument> Documents => Set<KnowledgeDocument>();
    public DbSet<DocumentAccessGrant> DocumentAccessGrants => Set<DocumentAccessGrant>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> Messages => Set<ConversationMessage>();
    public DbSet<MessageCitation> MessageCitations => Set<MessageCitation>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(b => { b.ToTable("Tenants"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200); });
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users"); b.HasKey(x => x.Id); b.Property(x => x.IdentityProviderSubject).HasMaxLength(300);
            b.Property(x => x.DisplayName).HasMaxLength(200); b.Property(x => x.Email).HasMaxLength(320);
            b.HasIndex(x => x.IdentityProviderSubject).IsUnique();
        });
        modelBuilder.Entity<TenantMembership>(b =>
        {
            b.ToTable("TenantMemberships"); b.HasKey(x => new { x.TenantId, x.UserId });
            b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<KnowledgeBase>(b =>
        {
            b.ToTable("KnowledgeBases"); b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(200);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<KnowledgeDocument>(b =>
        {
            b.ToTable("Documents"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.SourceUri).HasMaxLength(2000); b.Property(x => x.ContentType).HasMaxLength(200);
            b.Property(x => x.ContentHash).HasMaxLength(64); b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.TenantId, x.KnowledgeBaseId, x.ContentHash });
            b.HasMany(x => x.AccessGrants).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Chunks).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DocumentAccessGrant>(b =>
        {
            b.ToTable("DocumentAccessGrants"); b.HasKey(x => x.Id); b.Property(x => x.PrincipalId).HasMaxLength(300);
            b.HasIndex(x => new { x.TenantId, x.PrincipalType, x.PrincipalId });
        });
        modelBuilder.Entity<DocumentChunk>(b =>
        {
            b.ToTable("DocumentChunks"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.SourceUri).HasMaxLength(2000); b.HasIndex(x => new { x.TenantId, x.KnowledgeBaseId, x.DocumentId });
        });
        modelBuilder.Entity<IngestionJob>(b =>
        {
            b.ToTable("IngestionJobs"); b.HasKey(x => x.Id); b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.Status, x.CreatedAt });
        });
        modelBuilder.Entity<Conversation>(b =>
        {
            b.ToTable("Conversations"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => new { x.TenantId, x.UserId, x.UpdatedAt });
            b.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConversationMessage>(b =>
        {
            b.ToTable("Messages"); b.HasKey(x => x.Id); b.Property(x => x.Role).HasMaxLength(30);
            b.HasIndex(x => new { x.TenantId, x.ConversationId, x.Sequence }).IsUnique();
            b.HasMany(x => x.Citations).WithOne().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MessageCitation>(b =>
        {
            b.ToTable("MessageCitations"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.SourceUri).HasMaxLength(2000); b.Property(x => x.Excerpt).HasMaxLength(1000);
        });
        modelBuilder.Entity<AgentRun>(b =>
        {
            b.ToTable("AgentRuns"); b.HasKey(x => x.Id); b.Property(x => x.WorkflowName).HasMaxLength(200);
            b.Property(x => x.ModelName).HasMaxLength(200); b.HasIndex(x => new { x.TenantId, x.ConversationId, x.StartedAt });
        });
        modelBuilder.Entity<AgentSession>(b =>
        {
            b.ToTable("AgentSessions", t => t.HasCheckConstraint("CK_AgentSessions_StateJson_IsJson", "ISJSON([StateJson]) = 1"));
            b.HasKey(x => x.Id); b.Property(x => x.AgentName).HasMaxLength(200); b.Property(x => x.FrameworkVersion).HasMaxLength(50);
            b.Property(x => x.RowVersion).IsRowVersion(); b.HasIndex(x => new { x.TenantId, x.ConversationId, x.AgentName }).IsUnique();
        });
        modelBuilder.Entity<ApprovalRequest>(b =>
        {
            b.ToTable("ApprovalRequests", t => t.HasCheckConstraint("CK_ApprovalRequests_ArgumentsJson_IsJson", "ISJSON([ArgumentsJson]) = 1"));
            b.HasKey(x => x.Id); b.Property(x => x.ToolName).HasMaxLength(200); b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt });
        });
        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.ToTable("AuditEvents", t => t.HasCheckConstraint("CK_AuditEvents_DataJson_IsJson", "ISJSON([DataJson]) = 1"));
            b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.EventType).HasMaxLength(200);
            b.Property(x => x.ResourceType).HasMaxLength(100); b.Property(x => x.ResourceId).HasMaxLength(200);
            b.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });
    }
}
