# CLAUDE.md

Este arquivo é a constituição operacional do Cirth para o Claude Code. Leia antes de qualquer mudança. Mantenha curto, atualize quando padrões mudarem.

## Projeto

**Cirth** é uma plataforma pessoal de Knowledge Management com chat RAG. Stack 100% .NET 10, Blazor Server, modular monolith em Clean Architecture, containerizada via Docker Compose. Multi-tenant lógico desde V1 (cada usuário é um tenant). Pode virar SaaS para amigos depois.

O nome vem de **Cirth**, o sistema rúnico dos Anões na Terra-média. A identidade visual é arquivo de Gondor / Bodleian Library: tema escuro, ouro pergaminho, tipografia Cinzel + Inter.

## Stack obrigatória

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10, ASP.NET Core, **Razor Pages** (server-rendered) |
| UI Interactivity | **HTMX 2.x** (+ htmx-sse para chat streaming) |
| UI Styling | CSS custom com design tokens Cirth (`wwwroot/css/cirth.css`) |
| ORM | EF Core 10 com Npgsql |
| Mediator/CQRS | MediatR |
| LLM | Microsoft.Extensions.AI + Semantic Kernel, provider Azure AI Foundry |
| Embeddings | `text-embedding-3-small` via Azure AI Foundry |
| RDBMS | PostgreSQL 16 (com `pg_trgm` e `tsvector` para BM25-like) |
| Vector DB | Qdrant |
| Cache | Redis 7 (cache-aside) |
| Object Storage | MinIO (S3-compatible) |
| Reverse Proxy | NGINX + ModSecurity (OWASP CRS) |
| Auth | OIDC com Entra ID, single-tenant `c050c98c-b463-4591-ac3b-deb782c0ba6e` |
| Logs | Serilog estruturado (Console + File rolling) |
| Background | `IHostedService` + Hangfire-lite (apenas Postgres job queue própria) |
| MCP | `ModelContextProtocol` SDK oficial .NET |

Não introduza dependências fora desta lista sem ADR.

## Estrutura da solution

```
src/
  Cirth.Domain/          # Entidades, value objects, domain events. Zero deps externas.
  Cirth.Application/     # Use cases (MediatR handlers), DTOs, interfaces de portas. Deps: Domain + MediatR.
  Cirth.Infrastructure/  # EF Core, Qdrant client, MinIO client, LLM adapters, Identity. Deps: Application.
  Cirth.Web/             # Blazor Server + MudBlazor + páginas + auth UI. Deps: Application + Infrastructure.
  Cirth.Mcp/             # MCP server (stdio + HTTP). Reusa Application handlers. Deps: Application + Infrastructure.
  Cirth.Worker/          # BackgroundService de ingestão, embeddings, jobs. Deps: Application + Infrastructure.
  Cirth.Shared/          # Cross-cutting (Result<T>, exceptions, primitives).
tests/
  Cirth.Domain.Tests/
  Cirth.Application.Tests/
  Cirth.Integration.Tests/   # Testcontainers para Postgres/Redis/Qdrant/MinIO
```

**Regra de dependência inviolável**: dependências apontam para dentro (Web → Application → Domain). Domain não conhece Infrastructure.

## Convenções de código C#

- File-scoped namespaces obrigatório.
- `var` quando o tipo é óbvio na linha; tipo explícito quando agrega clareza.
- Nullable reference types `enable` em todos os projetos.
- Async sempre com `Async` suffix e `CancellationToken` no final.
- Records para DTOs imutáveis. Classes para entidades de domínio.
- Primary constructors quando reduzem ruído.
- Pattern matching e expression-bodied members onde melhora leitura.
- Nada de `Exception` genérica capturada sem rethrow ou tratamento específico.
- Result pattern (`Result<T>`) para fluxos esperados (validação, not found, business rule). Exceções apenas para excepcional de verdade.
- Não use `Task.Result` ou `.Wait()`. Tudo async até o topo.

## Convenções de domínio

- Tudo escopado a `TenantId`. Global query filter no EF Core garante isso.
- Entidades expõem comportamento, não setters públicos. `Document.AddTag(tag)`, não `document.Tags.Add(tag)`.
- IDs como `record struct DocumentId(Guid Value)` (tipo forte, evita misturar IDs).
- Domain events emitidos via `INotification` do MediatR, despachados no `SaveChangesAsync` do `AppDbContext`.

## Convenções de Application

- Cada use case = um `IRequest<Result<T>>` + um `IRequestHandler`.
- Pasta por feature: `Application/Features/Documents/UploadDocument/`.
- Validação via FluentValidation, plugada como `IPipelineBehavior`.
- Pipeline behaviors: Logging → Validation → Tenant scoping → Handler.

## Convenções de UI (Razor Pages + HTMX)

- Páginas em `Cirth.Web/Pages/`, organizadas por feature (ex.: `Pages/Documents/Index.cshtml`, `Pages/Documents/Upload.cshtml`).
- Cada página = par `Foo.cshtml` (view) + `Foo.cshtml.cs` (PageModel).
- PageModels herdam de `Cirth.Web.Infrastructure.CirthPageModel` (helpers: `Toast`, `SendAsync`, `HxRedirect`, `IsHtmx`).
- Layout único em `Pages/Shared/_Layout.cshtml`. Tema Cirth via `wwwroot/css/cirth.css` (classes utilitárias: `.btn`, `.card`, `.chip`, `.grid-N`, `.flex`, etc.). **Não usar frameworks CSS externos.**
- Parciais (`_Foo.cshtml`) ficam ao lado da página que os usa. Servem como targets HTMX para swap (`hx-target`, `hx-swap="outerHTML"` ou `"beforeend"`).
- HTMX patterns:
  - Form submit com partial swap: `<form hx-post="...?handler=X" hx-target="#list" hx-swap="outerHTML">`.
  - Debounced filtro: `hx-trigger="keyup changed delay:300ms"`.
  - Toast vindo do server: handler chama `Toast(...)` → seta header `HX-Trigger: {"toast":{...}}` que o `cirth.js` consome.
  - Redirect pós-POST: `HxRedirect("/path")` (envia `HX-Redirect` no HTMX, `302` fora dele).
  - **NUNCA** use `data-hx-*` em atributos lidos por JS custom — HTMX 2.x auto-processa `data-hx-*` como se fossem `hx-*`, causando duplo-fire de requests. Use nomes neutros (ex.: `data-load-url`) para metadados consumidos só pelo nosso JS.
  - **Antiforgery**: Razor Pages exige antiforgery em todos os POSTs. O `_Layout` injeta o token real via `IAntiforgery.GetAndStoreTokens(Context).RequestToken` no `hx-headers` do `<body>`. Isso cobre POSTs sem `<form>` (ex.: chips com `hx-post` no Documents/Upload). **Não esquecer**: novos POSTs precisam estar dentro da `<body>` do layout (qualquer página Razor Page herda automaticamente).
- Streaming de chat usa **SSE** (`text/event-stream`): POST cacheia o request em `IMemoryCache`, retorna HTML com `sse-connect`; endpoint GET `/Chat/Stream/{id}` consome `IAsyncEnumerable<string>` da Application e emite eventos `token` / `done`.
- **NÃO** chame Application handlers direto da view `.cshtml`. Sempre via PageModel.

## Comandos comuns

```bash
# Subir stack inteira
docker compose up -d

# Subir só infra (Postgres, Qdrant, Redis, MinIO) para dev local
docker compose -f docker-compose.infra.yml up -d

# Migrations
dotnet ef migrations add <Nome> -p src/Cirth.Infrastructure -s src/Cirth.Web
dotnet ef database update -p src/Cirth.Infrastructure -s src/Cirth.Web

# Rodar Web em watch
make watch

# Testes — ver seção abaixo para detalhes
make test               # rápido, sem Docker
make test-integration   # requer Docker
make test-all           # tudo

# Nova migration
make migration NAME=NomeDaMigration

# Build de produção
dotnet publish src/Cirth.Web -c Release -o publish/web
```

## Testes

O `Makefile` na raiz é o ponto de entrada canônico para testes:

| Comando | O que roda | Docker? |
|---|---|---|
| `make test` | Domain.Tests + Application.Tests | Não |
| `make test-integration` | Integration.Tests (Testcontainers) | Sim |
| `make test-all` | Todos os projetos | Sim |

Nunca use `dotnet test` sem path — o sln inclui integration tests que falham sem Docker.

- Unit tests para Domain e Application: xUnit + FluentAssertions + NSubstitute.
- Integration tests via Testcontainers para Postgres/Redis/Qdrant/MinIO. Sem mocks de infra real.
- Coverage mínimo aceito em Application: 70%. Domain: 90%. UI e Infrastructure: não exigido.
- Naming: `Method_Scenario_ExpectedBehavior`.

## Git

- Branch padrão: `main`.
- Branches de feature: `feat/<descrição-curta>`, `fix/<descrição>`, `chore/<descrição>`, `docs/<descrição>`.
- Conventional Commits: `feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`.
- PRs disparam o pipeline (build + test + docker build). Merge em main faz push da imagem para GHCR.
- Não commitar `.env`, segredos, binários, ou pastas `bin/obj/publish`.

## RAG: regras do jogo

- **Chunking**: usar `TextChunker` do Semantic Kernel. Tamanho padrão 800 tokens, overlap 100. Para Markdown/HTML/DOCX, chunk por estrutura primeiro, fallback para tamanho fixo.
- **Embedding**: `text-embedding-3-small` (1536 dim). Modelo configurável em `appsettings`, mas trocar exige reembedar tudo (não trocar de leve).
- **Busca híbrida**: SEMPRE combinar BM25 (Postgres `ts_rank_cd`) + vector (Qdrant cosine) via Reciprocal Rank Fusion (k=60). Não usar só um dos dois sem ADR.
- **Modelo de LLM por operação**:
  - Chat principal: `gpt-4.1`
  - Resumo de doc, classificação, tag suggestion: `gpt-4.1-mini`
  - Fallback de degradação: `gpt-4.1-nano` se disponível
- **Prompt template** centralizado em `Cirth.Infrastructure/Ai/Prompts/`. Não inline.
- **SavedAnswer**: antes de chamar LLM no chat, verificar `SavedAnswer` por similaridade (threshold 0.85). Se match, oferecer ao usuário antes de gastar token.
- **Quotas**: cada user tem quota de tokens/dia (`UserQuota`). Bloquear antes de chamar LLM. Default 100k tokens/dia.

## Segurança

- Secrets em dev: `dotnet user-secrets`. Em produção: `.env` lido pelo Docker Compose com permissão 600 no host.
- Nunca logar tokens, API keys, conteúdo de prompt do usuário (PII).
- Auth via Entra ID OIDC com PKCE. Cookie de sessão httpOnly, secure, sameSite=Lax.
- Toda query da Application já filtra por `TenantId` via global query filter. Não desabilitar sem revisar.
- Rate limit por user via `Microsoft.AspNetCore.RateLimiting`: 30 req/min para chat, 60 req/min para search, 5 uploads/min.
- NGINX na frente com ModSecurity + OWASP CRS rodando em modo `On` (bloqueio).

## Observabilidade V1

- Serilog estruturado: console (dev) + arquivo rolling (prod).
- Cada request tem `CorrelationId` propagado por middleware.
- Health checks em `/health` e `/health/ready` (checa Postgres, Qdrant, Redis, MinIO).
- Métricas Prometheus expostas em `/metrics` (já preparado, scraper opcional na V1).

## Quando ficar em dúvida

1. Re-leia a SPEC-V1 em `docs/SPEC-V1.md`.
2. Re-leia os ADRs em `docs/adr/`.
3. Se a dúvida exigir decisão arquitetural nova, escreva um ADR novo antes de codificar.
4. Mantenha simplicidade. Cirth V1 não é Google. É um KM pessoal que precisa funcionar bem.

## O que NÃO fazer

- Microsserviços. É monolito modular. Ponto.
- Cassandra, MongoDB, ElasticSearch. Postgres + Qdrant resolvem.
- gRPC entre módulos. Eles falam por chamada direta de handler MediatR.
- API REST pública na V1 (fica para V2).
- Mídia (vídeo/áudio) na V1 (fica para V1.5).
- Telas administrativas elaboradas. Foque em ingestão, busca, chat. Resto é mínimo.
- Frontend fora de Razor Pages + HTMX. Sem React, sem Angular, sem Vue, sem Blazor (migrado out por instabilidade do circuito SignalR).
- Bibliotecas pagas sem ADR.
