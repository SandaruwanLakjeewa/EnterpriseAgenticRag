# Architecture

## Runtime flow

```text
Client
  -> API authentication
  -> tenant/user/group identity derived from claims
  -> ConversationService
  -> bounded query planner
  -> IKnowledgeRetriever
       -> TenantId filter
       -> knowledge-base filter
       -> user/group document ACL filter
       -> ranking and top-k
  -> IAnswerGenerator
       -> Agent Framework + Azure OpenAI when configured
       -> extractive local fallback otherwise
  -> assistant message + citations + AgentRun persisted in SQL Server
```

The LLM never decides authorization. Tenant and document access filters are deterministic and execute before retrieved text is included in the prompt.

## Ingestion flow

```text
POST document
  -> document metadata/content + queued IngestionJob in SQL Server
  -> worker atomically claims oldest queued job
  -> overlapping chunker
  -> replace derived chunks
  -> document marked Indexed
  -> job marked Completed
```

The current input contract accepts text to keep the first slice easy to run. Introduce `IDocumentContentStore` and parsers for Blob Storage, PDF, Office, SharePoint, and web sources without putting binary files in relational tables.

## Database ownership

SQL Server is the durable system of record for tenants, application users, memberships, conversations, messages, agent runs, framework session snapshots, approvals, audit events, document metadata, permissions, chunks, and ingestion jobs.

`DocumentChunks` intentionally provides a simple local retrieval implementation. At enterprise corpus size, implement the existing `IKnowledgeRetriever` port with Azure AI Search or Qdrant and treat that store as a rebuildable index. Continue to store document ownership, versions, jobs, and application state in SQL Server.

## Important extension points

- `IKnowledgeRetriever`: hybrid/vector retrieval, semantic ranking, ACL-aware filtering.
- `IQueryPlanner`: replace bounded deterministic decomposition with structured LLM planning.
- `IAnswerGenerator`: model/provider adapter; currently Agent Framework and Azure OpenAI.
- `IDocumentChunker`: semantic, structure-aware, or model-assisted chunking.
- `ICurrentRequestIdentity`: trusted claims-to-application identity boundary.
- Repository interfaces: allow integration testing and alternative persistence implementations.

## Security invariants

1. Tenant and user identifiers come from authenticated claims, never request JSON.
2. Repository lookups include tenant and ownership filters.
3. Retrieval applies document ACLs before content reaches the model.
4. Retrieved content is treated as untrusted data in the agent prompt.
5. State-changing tools must create an expiring `ApprovalRequest` before execution.
6. Serialized Agent Framework session JSON is supplemental state, not the only business record.

## Next implementation increments

1. Add EF Core migrations and integration tests with a SQL Server test container.
2. Add Blob Storage and file parsing pipeline.
3. Add embeddings plus Qdrant or Azure AI Search hybrid retrieval.
4. Add SSE token streaming and resumable run events.
5. Implement approval endpoints and approved tool execution.
6. Add evaluation datasets for retrieval recall, groundedness, citation correctness, and tenant leakage.
