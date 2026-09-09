using EnterpriseAgenticRag.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseAgenticRag.Infrastructure.Persistence;

public static class DevelopmentDefaults
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid KnowledgeBaseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}

public static class DatabaseInitializer
{
    public static async Task InitializeDevelopmentDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnterpriseRagDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Tenants.AnyAsync(cancellationToken)) return;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        db.Tenants.Add(new Tenant { Id = DevelopmentDefaults.TenantId, Name = "Contoso Learning Tenant", CreatedAt = now });
        db.Users.Add(new ApplicationUser
        {
            Id = DevelopmentDefaults.UserId, IdentityProviderSubject = "development-user",
            DisplayName = "Development User", Email = "developer@contoso.local", CreatedAt = now
        });
        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = DevelopmentDefaults.TenantId, UserId = DevelopmentDefaults.UserId,
            Status = MembershipStatus.Active, JoinedAt = now
        });
        db.KnowledgeBases.Add(new KnowledgeBase
        {
            Id = DevelopmentDefaults.KnowledgeBaseId, TenantId = DevelopmentDefaults.TenantId,
            Name = "Enterprise Policies", Description = "Seed knowledge base for local learning.", CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
