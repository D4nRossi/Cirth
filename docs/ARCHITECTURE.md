# Cirth — Arquitetura

Documento técnico de arquitetura: diagramas C4 em PlantUML (readable, sem legenda/cores/protocolos — você gera a parte visual no draw.io reaproveitando o padrão visual TP), fluxos críticos em sequence diagrams, e notas de decisão.

---

## 1. C4 Context

```plantuml
@startuml C1_Context

actor "Danirock" as Owner
actor "Convidados" as Guests
actor "Claude Code / Desktop" as ClaudeAgent

rectangle "Cirth\nKnowledge Management Platform" as Cirth

rectangle "Azure AI Foundry\nLLM + Embeddings" as Foundry
rectangle "Entra ID\nSSO OIDC" as EntraID
rectangle "Backblaze B2\nBackup Storage" as B2
rectangle "Cloudflare Tunnel\n(quando exposto remoto)" as Cloudflare

Owner --> Cirth : usa via browser
Guests --> Cirth : usa via browser
ClaudeAgent --> Cirth : consulta via MCP

Cirth --> Foundry : embeddings + chat completions
Cirth --> EntraID : autenticação OIDC
Cirth --> B2 : backup semanal
Cirth <-- Cloudflare : reverse tunnel

@enduml
```

## 2. C4 Container

```plantuml
@startuml C2_Container

actor User
actor "Claude Agent" as Agent

rectangle "Cirth Platform" {
  
  Container_Boundary(edge, "Edge") {
    rectangle "NGINX\n+ ModSecurity (OWASP CRS)" as NGINX
  }
  
  Container_Boundary(app, "Application") {
    rectangle "Cirth.Web\nBlazor Server\n.NET 10" as Web
    rectangle "Cirth.Mcp\nMCP Server\n.NET 10" as Mcp
    rectangle "Cirth.Worker\nBackgroundService\n.NET 10" as Worker
  }
  
  Container_Boundary(data, "Data") {
    database "PostgreSQL 16\nMetadata + FTS\n+ Job Queue" as Postgres
    database "Qdrant\nVector Embeddings" as Qdrant
    database "Redis 7\nCache + Sessions" as Redis
    rectangle "MinIO\nS3-compatible\nObject Storage" as Minio
  }
}

rectangle "Azure AI Foundry" as Foundry
rectangle "Entra ID" as EntraID

User --> NGINX
Agent --> NGINX
NGINX --> Web
NGINX --> Mcp

Web --> Postgres
Web --> Qdrant
Web --> Redis
Web --> Minio
Web --> Foundry
Web --> EntraID

Mcp --> Postgres
Mcp --> Qdrant
Mcp --> Redis
Mcp --> Foundry

Worker --> Postgres
Worker --> Qdrant
Worker --> Minio
Worker --> Foundry

@enduml
```

## 3. C4 Component — Cirth.Application

```plantuml
@startuml C3_Application_Components

rectangle "Cirth.Application" {
  
  Container_Boundary(features, "Features (Use Cases)") {
    rectangle "Documents\n- UploadDocument\n- GetDocument\n- ListDocuments\n- DeleteDocument\n- RestoreDocumentVersion" as DocsFeat
    rectangle "Search\n- HybridSearch\n- FilteredSearch" as SearchFeat
    rectangle "Chat\n- StartConversation\n- SendMessage (streaming)\n- ListConversations\n- RegenerateMessage" as ChatFeat
    rectangle "SavedAnswers\n- SaveAnswer\n- SearchSavedAnswers\n- RateSavedAnswer" as SavedFeat
    rectangle "Tags\n- CreateTag\n- AssignTags\n- SuggestTags (AI)" as TagsFeat
    rectangle "Collections\n- CreateCollection\n- AddToCollection" as CollFeat
    rectangle "Identity\n- ProvisionUserOnLogin\n- InviteUser\n- ManageRoles\n- GenerateApiKey" as IdentFeat
    rectangle "Quotas\n- CheckQuota\n- ConsumeQuota\n- ResetDaily" as QuotaFeat
  }
  
  Container_Boundary(ports, "Ports (Interfaces)") {
    rectangle "IDocumentParser\nIChunker\nIEmbeddingService\nIVectorStore\nIObjectStorage\nILlmChatService\nIJobQueue\nIApiKeyHasher" as Ports
  }
  
  Container_Boundary(pipeline, "Pipeline Behaviors") {
    rectangle "LoggingBehavior\nValidationBehavior\nTenantScopingBehavior\nQuotaBehavior" as Pipeline
  }
}

DocsFeat ..> Ports
SearchFeat ..> Ports
ChatFeat ..> Ports
SavedFeat ..> Ports
TagsFeat ..> Ports

@enduml
```

## 4. Fluxo: Ingestão de documento

```plantuml
@startuml Seq_Ingestion

actor User
participant "Cirth.Web\nUploadPage" as Web
participant "Application\nUploadDocument" as UploadUC
participant "MinIO" as Minio
participant "Postgres" as DB
participant "Cirth.Worker" as Worker
participant "Parser" as Parser
participant "Chunker" as Chunker
participant "Foundry\nEmbeddings" as Emb
participant "Qdrant" as Qdrant
participant "SignalR Hub" as Hub

User -> Web : upload arquivo
Web -> UploadUC : UploadDocumentCommand
UploadUC -> Minio : PutObject(bucket, key)
UploadUC -> DB : INSERT documents, document_versions
UploadUC -> DB : INSERT jobs(type=ProcessDocument)
UploadUC --> Web : DocumentId
Web --> User : "Em processamento"

Worker -> DB : poll jobs WHERE status=Pending
Worker -> Minio : GetObject
Worker -> Parser : extract text
Worker -> Chunker : split (800 tokens, overlap 100)
loop chunks
  Worker -> Emb : embed(chunk)
  Worker -> Qdrant : upsert point
  Worker -> DB : INSERT chunks
end
Worker -> DB : UPDATE documents SET status=Indexed
Worker -> Hub : NotifyIndexed(tenantId, userId, documentId)
Hub -> Web : push via SignalR
Web --> User : toast "Documento indexado"

@enduml
```

## 5. Fluxo: Busca híbrida

```plantuml
@startuml Seq_Search

actor User
participant "Cirth.Web" as Web
participant "Application\nHybridSearch" as Search
participant "Postgres\nFTS" as PG
participant "Foundry\nEmbeddings" as Emb
participant "Qdrant" as Qdrant
participant "RRF Merger" as RRF

User -> Web : query
Web -> Search : HybridSearchQuery
Search -> PG : SELECT chunks tsvector @@ tsquery LIMIT 50
Search -> Emb : embed(query)
Search -> Qdrant : search top 50 by cosine
Search -> RRF : merge by reciprocal rank fusion (k=60)
RRF --> Search : top 8 results
Search -> PG : ts_headline para highlights
Search --> Web : results + highlights
Web --> User : render results

@enduml
```

## 6. Fluxo: Chat RAG com streaming

```plantuml
@startuml Seq_Chat

actor User
participant "Cirth.Web\nChatPage" as Web
participant "Application\nSendMessage" as Chat
participant "QuotaCheck" as Quota
participant "SavedAnswerLookup" as Saved
participant "HybridSearch" as Search
participant "PromptBuilder" as Prompt
participant "Foundry\nLLM" as LLM
participant "Postgres" as DB

User -> Web : envia mensagem
Web -> Chat : SendMessageCommand
Chat -> Quota : CheckUserQuota
Quota --> Chat : OK
Chat -> Saved : SearchByEmbedding(question, threshold=0.85)
alt SavedAnswer match
  Saved --> Chat : SavedAnswer
  Chat --> Web : offer reuse
else no match
  Chat -> Search : HybridSearch(question, K=8)
  Search --> Chat : 8 chunks
  Chat -> Prompt : build(system, context, history, user)
  Chat -> LLM : ChatCompletion(stream=true)
  loop tokens
    LLM --> Chat : token
    Chat --> Web : SignalR push
    Web --> User : render token
  end
  Chat -> DB : INSERT messages (assistant)
  Chat -> Quota : ConsumeTokens
end

@enduml
```

## 7. Fluxo: MCP server respondendo

```plantuml
@startuml Seq_Mcp

actor "Claude Agent" as Agent
participant "NGINX" as NG
participant "Cirth.Mcp" as Mcp
participant "ApiKeyAuth" as Auth
participant "Application Handler" as Handler

Agent -> NG : POST /mcp/tools/call (X-Cirth-Api-Key)
NG -> Mcp : forward
Mcp -> Auth : validate key
Auth --> Mcp : (tenantId, userId)
Mcp -> Handler : dispatch (mesma handler usada pela Web)
Handler --> Mcp : result
Mcp --> Agent : MCP response

@enduml
```

## 8. Decisões arquiteturais importantes

Consulte `docs/adr/` para o histórico completo. Resumo:

- **ADR-001**: Modular monolith em Clean Architecture, não microsserviços.
- **ADR-002**: Blazor Server como frontend único, sem SPA externa.
- **ADR-003**: Busca híbrida BM25 + vetorial via RRF, sem reranker na V1.
- **ADR-004**: MinIO como object storage S3-compatible desde o início.
- **ADR-005**: Multi-tenant lógico via TenantId + global query filter desde a V1.
- **ADR-006**: MCP server reusa Application handlers (mesma lógica que UI).

## 9. Topologia de deploy (V1)

```plantuml
@startuml Deploy

node "VM Linux (2vCPU/4GB)" {
  rectangle "Docker Engine" {
    rectangle "nginx (host net 80/443)" as NG
    rectangle "cirth-web" as Web
    rectangle "cirth-mcp" as Mcp
    rectangle "cirth-worker" as Worker
    database "postgres" as PG
    database "qdrant" as Qd
    database "redis" as Rd
    rectangle "minio" as Mn
  }
  rectangle "rclone (cron weekly)" as Backup
}

cloud "Internet" {
  rectangle "Cloudflare Tunnel" as CF
  rectangle "Backblaze B2" as B2
  rectangle "Azure AI Foundry" as AI
  rectangle "Entra ID" as EID
}

CF --> NG
Backup --> B2
Web --> AI
Web --> EID
Worker --> AI

@enduml
```

## 10. Estrutura de rede Docker

- Network `cirth-edge`: nginx + web + mcp
- Network `cirth-data`: web + mcp + worker + postgres + qdrant + redis + minio
- Network `cirth-internal`: web + worker (jobs hub)

NGINX é o único container exposto ao host.
