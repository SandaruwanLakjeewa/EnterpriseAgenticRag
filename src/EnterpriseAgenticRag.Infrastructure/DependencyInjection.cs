using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Infrastructure.AI;
using EnterpriseAgenticRag.Infrastructure.Persistence;
using EnterpriseAgenticRag.Infrastructure.Retrieval;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

namespace EnterpriseAgenticRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEnterpriseRagInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("EnterpriseRag")
            ?? throw new InvalidOperationException("ConnectionStrings:EnterpriseRag is required.");
        services.AddDbContext<EnterpriseRagDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        services.AddScoped<SqlRepositories>();
        services.AddScoped<IConversationRepository>(sp => sp.GetRequiredService<SqlRepositories>());
        services.AddScoped<IKnowledgeRepository>(sp => sp.GetRequiredService<SqlRepositories>());
        services.AddScoped<IAgentRunRepository>(sp => sp.GetRequiredService<SqlRepositories>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SqlRepositories>());
        services.AddScoped<IKnowledgeRetriever, SqlKnowledgeRetriever>();
        services.AddScoped<ConversationService>();
        services.AddScoped<KnowledgeService>();
        services.AddScoped<IngestionService>();
        services.AddSingleton<IQueryPlanner, BoundedQueryPlanner>();
        services.AddSingleton<IDocumentChunker>(_ => new OverlappingTextChunker());
        services.AddSingleton<IClock, SystemClock>();

        string? endpoint = configuration["AzureOpenAI:Endpoint"];
        string? deployment = configuration["AzureOpenAI:ChatDeployment"];
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri) && !string.IsNullOrWhiteSpace(deployment))
        {
            string? apiKey = configuration["AzureOpenAI:ApiKey"];
            AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
                ? new AzureOpenAIClient(endpointUri, new DefaultAzureCredential())
                : new AzureOpenAIClient(endpointUri, new AzureKeyCredential(apiKey));
            AIAgent agent = client.GetChatClient(deployment).AsAIAgent(
                name: "EnterpriseRagAnswerAgent",
                instructions: "You are a secure enterprise knowledge assistant. Use only supplied evidence and cite it precisely.");
            services.AddSingleton(agent);
            services.AddSingleton<IAnswerGenerator, AgentFrameworkAnswerGenerator>();
        }
        else
        {
            services.AddSingleton<IAnswerGenerator, ExtractiveAnswerGenerator>();
        }

        return services;
    }
}
