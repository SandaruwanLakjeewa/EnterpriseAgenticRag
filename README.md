# Enterprise Agentic RAG

A learning-oriented, enterprise-shaped Agentic RAG application built with .NET 10, Microsoft Agent Framework, SQL Server, EF Core, ASP.NET Core, and OpenTelemetry.

This is a separate solution from `develop-agents`. It implements a complete first vertical slice: authenticated multi-tenant API, conversations, durable agent runs, document ingestion, permission-aware retrieval, citations, and an optional Azure OpenAI answer agent.

## Projects

| Project | Responsibility |
|---|---|
| `EnterpriseAgenticRag.Domain` | Persistence-independent entities and state enums |
| `EnterpriseAgenticRag.Application` | Use cases, ports, RAG orchestration, query planning, chunking |
| `EnterpriseAgenticRag.Infrastructure` | EF Core/SQL Server, secured retrieval, Agent Framework adapter |
| `EnterpriseAgenticRag.Contracts` | Stable HTTP request/response contracts |
| `EnterpriseAgenticRag.Api` | Authentication, authorization, endpoints, error mapping |
| `EnterpriseAgenticRag.Ingestion.Worker` | Asynchronous document chunking/indexing |
| `EnterpriseAgenticRag.ServiceDefaults` | Health checks, HTTP resilience, OpenTelemetry |
| `tests/*` | Unit and dependency-boundary tests |

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the flow and extension points.

## Run locally

Prerequisites: .NET 10 SDK and Docker Desktop.

```powershell
docker compose up -d
dotnet run --project src/EnterpriseAgenticRag.Ingestion.Worker
dotnet run --project src/EnterpriseAgenticRag.Api
```

The API listens at `http://localhost:5227`. Use `src/EnterpriseAgenticRag.Api/EnterpriseAgenticRag.Api.http` for the end-to-end flow.

Development authentication automatically supplies the seeded tenant and user. Never use `Authentication:Mode=Development` outside a local environment.

## Azure OpenAI

Without Azure OpenAI configuration, the app returns an extractive development response so the complete ingestion/retrieval flow remains testable. Configure the agent with environment variables:

```powershell
$env:AzureOpenAI__Endpoint = "https://your-resource.openai.azure.com/"
$env:AzureOpenAI__ChatDeployment = "your-chat-deployment"
```

`DefaultAzureCredential` is used when no API key is configured. For local learning, authenticate with `az login`. Do not commit API keys.

## Production changes before deployment

- Set `Authentication:Mode` to `Entra`, and configure authority and audience.
- Replace `EnsureCreated` with reviewed EF Core migrations.
- Store connection strings and secrets in a managed secret store.
- Replace `SqlKnowledgeRetriever` with hybrid vector retrieval using Azure AI Search or Qdrant.
- Store original binary documents in object storage; retain SQL Server as metadata/system of record.
- Add distributed per-conversation locking, rate limiting, content safety, evaluation gates, and immutable audit export.
- Keep document ACL filtering in the retrieval implementation before any content reaches the model.
