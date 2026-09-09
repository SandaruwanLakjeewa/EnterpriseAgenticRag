using EnterpriseAgenticRag.Domain;

namespace EnterpriseAgenticRag.Application;

public sealed class ConversationService(
    ICurrentRequestIdentity currentIdentity,
    IConversationRepository conversations,
    IKnowledgeRepository knowledge,
    IAgentRunRepository runs,
    IQueryPlanner planner,
    IKnowledgeRetriever retriever,
    IAnswerGenerator answerGenerator,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ConversationResult> CreateAsync(CreateConversationCommand command, CancellationToken cancellationToken)
    {
        RequestIdentity identity = currentIdentity.GetRequired();
        if (!await knowledge.KnowledgeBaseExistsAsync(identity.TenantId, command.KnowledgeBaseId, cancellationToken))
        {
            throw new KeyNotFoundException("Knowledge base was not found.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            TenantId = identity.TenantId,
            UserId = identity.UserId,
            KnowledgeBaseId = command.KnowledgeBaseId,
            Title = string.IsNullOrWhiteSpace(command.Title) ? "New conversation" : command.Title.Trim(),
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        await conversations.AddAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(conversation.Id, conversation.KnowledgeBaseId, conversation.Title, conversation.CreatedAt);
    }

    public async Task<ChatResult> SendMessageAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Message))
        {
            throw new ArgumentException("Message is required.", nameof(command));
        }

        RequestIdentity identity = currentIdentity.GetRequired();
        Conversation conversation = await conversations.GetAsync(identity.TenantId, identity.UserId, command.ConversationId, cancellationToken)
            ?? throw new KeyNotFoundException("Conversation was not found.");

        var input = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            TenantId = identity.TenantId,
            ConversationId = conversation.Id,
            Role = "user",
            Content = command.Message.Trim(),
            Sequence = conversation.Messages.Count + 1,
            CreatedAt = clock.UtcNow
        };
        conversation.Messages.Add(input);

        var run = new AgentRun
        {
            Id = Guid.NewGuid(),
            TenantId = identity.TenantId,
            ConversationId = conversation.Id,
            InputMessageId = input.Id,
            Status = AgentRunStatus.Running,
            StartedAt = clock.UtcNow
        };
        await runs.AddAsync(run, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            IReadOnlyList<string> queries = await planner.PlanAsync(input.Content, cancellationToken);
            var evidence = new Dictionary<Guid, RetrievedChunk>();
            foreach (string query in queries)
            {
                var request = new RetrievalRequest(identity.TenantId, identity.UserId, identity.GroupIds, conversation.KnowledgeBaseId, query);
                foreach (RetrievedChunk chunk in await retriever.RetrieveAsync(request, cancellationToken))
                {
                    evidence.TryAdd(chunk.ChunkId, chunk);
                }
            }

            IReadOnlyList<RetrievedChunk> rankedEvidence = evidence.Values.OrderByDescending(x => x.Score).Take(8).ToArray();
            string answer = await answerGenerator.GenerateAsync(new(input.Content, conversation.Messages, rankedEvidence), cancellationToken);
            var output = new ConversationMessage
            {
                Id = Guid.NewGuid(),
                TenantId = identity.TenantId,
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = answer,
                Sequence = conversation.Messages.Count + 1,
                CreatedAt = clock.UtcNow,
                Citations = rankedEvidence.Select(x => new MessageCitation
                {
                    Id = Guid.NewGuid(),
                    DocumentId = x.DocumentId,
                    ChunkId = x.ChunkId,
                    Title = x.Title,
                    SourceUri = x.SourceUri,
                    Excerpt = x.Content.Length <= 240 ? x.Content : x.Content[..240] + "..."
                }).ToList()
            };

            conversation.Messages.Add(output);
            conversation.UpdatedAt = clock.UtcNow;
            run.OutputMessageId = output.Id;
            run.Status = AgentRunStatus.Completed;
            run.CompletedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new(run.Id, output.Id, "completed", answer,
                output.Citations.Select(x => new CitationResult(x.DocumentId, x.ChunkId, x.Title, x.SourceUri, x.Excerpt)).ToArray());
        }
        catch
        {
            run.Status = AgentRunStatus.Failed;
            run.ErrorCode = "agent_run_failed";
            run.CompletedAt = clock.UtcNow;
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
