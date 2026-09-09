using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Domain;

namespace EnterpriseAgenticRag.ArchitectureTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void DomainDoesNotReferenceInfrastructureFrameworks()
    {
        string[] references = typeof(Tenant).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, x => x.StartsWith("Microsoft.Agents", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructure()
    {
        string[] references = typeof(ConversationService).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain("EnterpriseAgenticRag.Infrastructure", references);
    }
}
