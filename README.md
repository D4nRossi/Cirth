# Cirth

> *"Inscriba seu conhecimento. Consulte como Gandalf."*

**Cirth** é uma plataforma pessoal de Knowledge Management com chat RAG, batizada em homenagem ao sistema rúnico dos Anões da Terra-média, usado em Moria e nos registros do Livro de Mazarbul. Aqui, você não armazena documentos: você os inscreve.

## Visão

Um espaço único onde toda a sua base de conhecimento (PDFs técnicos, artigos, notas, decisões arquiteturais, documentações internas) é indexada, pesquisável por significado, e consultável em linguagem natural via chat com IA. Multi-usuário desde o nascimento, com perspectiva de virar SaaS leve para amigos próximos.

## Stack

- **Backend**: .NET 10, ASP.NET Core, Blazor Server, MudBlazor
- **IA**: Microsoft.Extensions.AI + Semantic Kernel, Azure AI Foundry (GPT-4.1, GPT-4.1-mini), embeddings `text-embedding-3-small`
- **Persistência**: PostgreSQL 16, Qdrant, Redis 7, MinIO
- **Infra**: Docker Compose, NGINX + ModSecurity (OWASP CRS)
- **Auth**: Entra ID OIDC, single-tenant
- **MCP**: servidor MCP oficial para consulta via Claude Desktop e Claude Code

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

- [`CLAUDE.md`](CLAUDE.md) — constituição operacional para Claude Code
- [`docs/SPEC-V1.md`](docs/SPEC-V1.md) — especificação completa da V1
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — diagramas C4 e fluxos
- [`docs/DESIGN-SYSTEM.md`](docs/DESIGN-SYSTEM.md) — paleta, tipografia, componentes
- [`docs/adr/`](docs/adr/) — Architecture Decision Records

## Roadmap

- **V1** — Texto, busca híbrida, chat RAG, MCP server, multi-tenant, CI/CD
- **V1.5** — Mídia (vídeo e áudio com STT batch via Azure AI Speech)
- **V2** — API REST pública versionada, observabilidade full (OTel + LGTM), agentes, semantic chunking avançado

## Quick start

```bash
cp .env.example .env
# edite .env com suas credenciais Azure / Entra ID

docker compose up -d
```

Acesse `https://cirth.local` (após adicionar no `/etc/hosts`).

## Licença

A definir. Provavelmente MIT quando virar público de verdade.
