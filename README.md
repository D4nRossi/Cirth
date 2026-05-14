# Cirth

> *"Inscriba seu conhecimento. Consulte como Gandalf."*

**Cirth** é uma plataforma pessoal de Knowledge Management com chat RAG, batizada em homenagem ao sistema rúnico dos Anões da Terra-média, usado em Moria e nos registros do Livro de Mazarbul. Aqui, você não armazena documentos: você os inscreve.

## Visão

Um espaço único onde toda a sua base de conhecimento (PDFs técnicos, artigos, notas, decisões arquiteturais, documentações internas) é indexada, pesquisável por significado, e consultável em linguagem natural via chat com IA. Multi-usuário desde o nascimento, com perspectiva de virar SaaS leve para amigos próximos.

## Stack

- **Backend**: .NET 10, ASP.NET Core, **Razor Pages + HTMX** (server-rendered; CSS custom em `wwwroot/css/cirth.css` sem framework UI)
- **IA**: Microsoft.Extensions.AI + Semantic Kernel, **Azure AI Foundry com endpoints duais** — Chat via OpenAI SDK puro contra `*.openai.azure.com/openai/v1` (Responses API) usando `gpt-4.1`/`gpt-4.1-mini`; Embeddings via Azure.AI.OpenAI contra `*.cognitiveservices.azure.com` usando `text-embedding-ada-002` (1536 dim)
- **Persistência**: PostgreSQL 16 (com `tsvector` GENERATED + GIN para BM25), Qdrant (Cosine, multi-tenant via filter por `tenant_id`), Redis 7, MinIO
- **Infra**: Docker Compose, NGINX + ModSecurity (OWASP CRS)
- **Auth**: Entra ID OIDC, single-tenant
- **MCP**: servidor MCP oficial para consulta via Claude Desktop e Claude Code
- **Jobs**: fila própria em Postgres (sem mensageria); `StuckJobRecoveryService` recupera jobs órfãos a cada 2min

## Arquitetura em uma frase

Modular monolith em Clean Architecture, multi-tenant lógico, busca híbrida BM25 + vetorial, RAG com streaming, MCP server gêmeo da UI.

## Identidade visual

Inspirada nos arquivos de Gondor cruzados com a Bodleian Library. Pergaminho-escuro elegante, ouro de selo, tipografia Cinzel para títulos e Inter para corpo. Tema fixo em dark.

| Token | Cor |
|---|---|
| `bg.deep` | `#0F0D0A` |
| `bg.surface` | `#1A1410` |
| `gold.primary` | `#C9A961` |
| `rune.red` | `#8B2500` |
| `moss.green` | `#5D7B3F` |
| `text.primary` | `#E8DCC4` |

Detalhes completos em [`docs/DESIGN-SYSTEM.md`](docs/DESIGN-SYSTEM.md).

## Documentação

- [`STATUS.md`](STATUS.md) — **comece aqui** quando voltar ao projeto: snapshot do estado atual, como retomar, TODO mapeado, regras a observar
- [`RUN.md`](RUN.md) — guia de execução local (secrets, comandos `make`, checklist de testes manuais, troubleshooting)
- [`CLAUDE.md`](CLAUDE.md) — constituição operacional para Claude Code
- [`docs/SPEC-V1.md`](docs/SPEC-V1.md) — especificação completa da V1
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — diagramas C4, fluxos, indexação de dados
- [`docs/DESIGN-SYSTEM.md`](docs/DESIGN-SYSTEM.md) — paleta, tipografia, componentes
- [`docs/adr/`](docs/adr/) — Architecture Decision Records

## Roadmap

- **V1** — Texto, busca híbrida, chat RAG, MCP server, multi-tenant, CI/CD
- **V1.5** — Mídia (vídeo e áudio com STT batch via Azure AI Speech)
- **V2** — API REST pública versionada, observabilidade full (OTel + LGTM), agentes, semantic chunking avançado

## Quick start (dev local)

```bash
# 1. Infra (Postgres, Redis, Qdrant, MinIO)
docker compose -f docker-compose.infra.yml up -d

# 2. Secrets do dev (uma vez)
dotnet user-secrets set AzureAi:Chat:Endpoint      "https://<resource>.openai.azure.com/openai/v1"        --project src/Cirth.Web
dotnet user-secrets set AzureAi:Chat:ApiKey        "<chave>"                                              --project src/Cirth.Web
dotnet user-secrets set AzureAi:Embedding:Endpoint "https://<resource>.cognitiveservices.azure.com/"      --project src/Cirth.Web
dotnet user-secrets set AzureAi:Embedding:ApiKey   "<chave>"                                              --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientId           "<client-id>"                                          --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientSecret       "<client-secret>"                                      --project src/Cirth.Web

# 3. Migrations + run
make db-update
make watch                                # https://localhost:5001 (Web com hot reload)
make worker                               # Worker (em outro terminal)
make logs-web                             # tail dos logs (em outro terminal)
```

Detalhes completos (incluindo App Registration no Entra ID e checklist de testes manuais) em [`RUN.md`](RUN.md).

### Stack inteira em Docker

```bash
cp .env.example .env && chmod 600 .env    # ajustar com endpoints e chaves
docker compose up -d                       # Web + Worker + Mcp + Nginx + infra
```

Acesse `https://cirth.local` (após adicionar no `/etc/hosts`).

## Licença

A definir. Provavelmente MIT quando virar público de verdade.
