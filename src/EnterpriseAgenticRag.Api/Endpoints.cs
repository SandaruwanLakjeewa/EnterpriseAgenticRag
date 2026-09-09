using EnterpriseAgenticRag.Application;
using EnterpriseAgenticRag.Contracts;

namespace EnterpriseAgenticRag.Api;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapEnterpriseRagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder api = endpoints.MapGroup("/api/v1").RequireAuthorization();

        api.MapPost("/knowledge-bases", async (CreateKnowledgeBaseRequest request, KnowledgeService service, CancellationToken ct) =>
        {
            KnowledgeBaseResult result = await service.CreateKnowledgeBaseAsync(new(request.Name, request.Description), ct);
            return Results.Created($"/api/v1/knowledge-bases/{result.Id}", new KnowledgeBaseResponse(result.Id, result.Name, result.Description, result.CreatedAt));
        });

        api.MapPost("/knowledge-bases/{knowledgeBaseId:guid}/documents", async (
            Guid knowledgeBaseId, SubmitTextDocumentRequest request, KnowledgeService service, CancellationToken ct) =>
        {
            SubmitDocumentResult result = await service.SubmitDocumentAsync(new(
                knowledgeBaseId, request.Title, request.Content, request.SourceUri, request.IsPublicWithinTenant,
                request.AllowedGroupIds ?? []), ct);
            return Results.Accepted($"/api/v1/ingestion-jobs/{result.JobId}", new SubmitDocumentResponse(result.DocumentId, result.JobId, result.Status));
        });

        api.MapGet("/ingestion-jobs/{jobId:guid}", async (Guid jobId, KnowledgeService service, CancellationToken ct) =>
        {
            IngestionJobResult result = await service.GetJobAsync(jobId, ct);
            return Results.Ok(new IngestionJobResponse(result.Id, result.DocumentId, result.Status, result.AttemptCount, result.Error));
        });

        api.MapPost("/conversations", async (CreateConversationRequest request, ConversationService service, CancellationToken ct) =>
        {
            ConversationResult result = await service.CreateAsync(new(request.KnowledgeBaseId, request.Title), ct);
            return Results.Created($"/api/v1/conversations/{result.Id}", new ConversationResponse(result.Id, result.KnowledgeBaseId, result.Title, result.CreatedAt));
        });

        api.MapPost("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId, SendMessageRequest request, ConversationService service, CancellationToken ct) =>
        {
            ChatResult result = await service.SendMessageAsync(new(conversationId, request.Message), ct);
            return Results.Ok(new ChatResponse(result.RunId, result.MessageId, result.Status, result.Answer,
                result.Citations.Select(x => new CitationResponse(x.DocumentId, x.ChunkId, x.Title, x.SourceUri, x.Excerpt)).ToArray()));
        });

        return endpoints;
    }
}
