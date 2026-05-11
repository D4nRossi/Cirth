# Cirth — Especificação V1

**Versão**: 1.0.0-draft
**Status**: Aprovado para implementação
**Owner**: Daniel Rossi de Amorim (Danirock)
**Última atualização**: 2026-05-11

---

## 1. Visão e objetivos

Cirth é uma plataforma pessoal de Knowledge Management com chat RAG, desenhada para uso individual com fundação para virar SaaS leve. O objetivo da V1 é entregar um MVP funcional completo capaz de:

1. Ingerir documentos textuais variados (PDF, DOCX, Markdown, TXT, HTML, links web).
2. Indexá-los em busca híbrida (BM25 lexical + vetorial semântica).
3. Permitir chat conversacional com RAG sobre o acervo, com streaming.
4. Salvar respostas para reuso sem chamar LLM novamente.
5. Expor a base como servidor MCP para consulta via Claude Desktop e Claude Code.
6. Operar containerizado, deployável numa VM única.

**Não-objetivos da V1**:
- Não é Notion. Edição rica de documentos não é foco.
- Não é Google Drive. Não há colaboração em tempo real.
- Não é agente. Cirth responde, não age.

## 2. Personas

| Persona | Descrição | Prioridade V1 |
|---|---|---|
| **Arquiteto-curador** (Danirock) | Owner do conhecimento, ingere e consulta diariamente | P0 |
| **Convidado-consultor** (amigos) | Recebe acesso, consulta a base, faz upload limitado | P1 |
| **Agente MCP** (Claude Code/Desktop) | Consome a base programaticamente | P0 |

## 3. Escopo V1

### Dentro

- Upload de documentos via UI: PDF, DOCX, MD, TXT, HTML, URL web.
- Parsing, chunking, embedding e indexação automáticos.
- Busca híbrida com filtros por tag, data, coleção, autor (campo livre).
- Chat RAG conversacional com streaming token-a-token.
- Salvamento de respostas (SavedAnswer) com sugestão de reuso.
- Versionamento de documentos (re-upload cria versão nova).
- Tagueamento manual e sugerido por IA.
- Coleções/folders simples (não-hierárquicos na V1).
- Multi-tenant lógico, multi-user com roles `Admin` e `User`.
- SSO via Entra ID OIDC.
- Servidor MCP expondo ferramentas de busca e consulta.
- Quotas de tokens LLM por usuário/dia.
- Rate limiting por endpoint.
- Backup automático Postgres + MinIO para Backblaze B2.
- CI/CD via GitHub Actions + GHCR.

### Fora (V1.5)

- Ingestão de vídeo e áudio (requer STT pipeline).
- Anotações inline em PDFs.
- Compartilhamento granular de documentos individuais.

### Fora (V2)

- API REST pública versionada.
- Observabilidade completa (OTel + LGTM).
- Reranker pós-retrieval.
- Semantic chunking baseado em similaridade.
- Agentes (multi-turn task execution).
- Billing e multi-tenancy comercial real.

## 4. Requisitos funcionais

### RF-01 Ingestão de documentos
- O usuário faz upload de um arquivo via UI.
- O sistema valida tamanho (máx 50MB), formato e quota.
- Cria registro `Document` em estado `Pending`.
- Worker assíncrono: parseia, faz chunk, gera embeddings, indexa em Qdrant, armazena arquivo bruto em MinIO.
- Notifica usuário via SignalR quando completa.
- Em caso de erro, documento fica em estado `Failed` com mensagem.

### RF-02 Busca
- Campo de busca em qualquer página.
- Resultados híbridos: BM25 do Postgres FTS + similaridade Qdrant, combinados por RRF.
- Filtros: tags, coleção, autor, intervalo de data, tipo de documento.
- Preview do trecho relevante com highlight.
- Click no resultado abre detalhe do documento na posição do chunk.

### RF-03 Chat RAG
- Tela de chat com lista de conversas anteriores no lateral.
- Mensagem enviada → retrieve top-K chunks (default K=8) → prompt → LLM stream → resposta na UI.
- Citações inline numeradas, cada uma linkando para o chunk de origem.
- Botão "Salvar resposta" promove a `SavedAnswer`.
- Botão "Regenerar" refaz a chamada com mesma pergunta.
- Antes de chamar LLM, verifica `SavedAnswer` por similaridade ≥ 0.85. Se match, mostra opção ao usuário.

### RF-04 Tagging
- Tags manuais: usuário adiciona livremente.
- Tag sugerida por IA: ao terminar ingestão, classification job sugere 3-5 tags com `gpt-4.1-mini`. Usuário aceita ou rejeita.
- Tags são scoped por tenant.

### RF-05 Coleções
- Coleção = agrupamento de documentos, flat (sem hierarquia).
- Usuário cria, renomeia, deleta coleções.
- Documento pode estar em múltiplas coleções (relação N:N).

### RF-06 Versionamento
- Re-upload de documento com nome similar (match por content hash ou trigger explícito do usuário) cria nova `DocumentVersion`.
- Embeddings da versão antiga: `is_current = false`. Continuam pesquisáveis se filtro "incluir versões antigas" estiver ativo (default off).
- Histórico visível em página de detalhe do documento.

### RF-07 SavedAnswer
- Item independente, scoped por tenant.
- Armazena: pergunta original, resposta, chunks citados, tags, contador de uso, score de utilidade (thumbs up/down).
- Aparece em buscas com badge "Resposta".
- Tem TTL configurável (default sem expiração).

### RF-08 Autenticação e autorização
- Login OIDC com Entra ID.
- Primeiro usuário do tenant vira `Admin`.
- Admin pode convidar outros usuários (email do Entra ID).
- Usuários convidados começam como `User`.
- `Admin` pode promover/demover usuários, ver todos os documentos da tenant, ajustar quotas.
- `User` vê apenas seus próprios documentos e os marcados como `Shared` na tenant.

### RF-09 Servidor MCP
- Servidor MCP standalone no projeto `Cirth.Mcp`, executável tanto em stdio quanto HTTP/SSE.
- Tools expostas:
  - `search_documents(query, tags?, limit?)` — busca híbrida
  - `ask_question(question, conversation_id?)` — fluxo RAG completo
  - `get_document(document_id)` — retorna metadata + conteúdo full
  - `list_tags()` — todas as tags da tenant
  - `list_collections()` — todas as coleções
  - `list_saved_answers(query?)` — busca em respostas salvas
- Autenticação via API Key gerada na UI por usuário (header `X-Cirth-Api-Key`).

### RF-10 Quotas e rate limiting
- `UserQuota`: tokens LLM diários (default 100k), uploads/dia (default 50), storage total (default 5GB).
- Rate limit: chat 30 req/min, search 60 req/min, upload 5 req/min.
- Excedeu: 429 com `Retry-After` e mensagem clara na UI.

## 5. Requisitos não-funcionais

| Categoria | Requisito |
|---|---|
| Performance | Busca p95 ≤ 500ms para acervo ≤ 10k chunks |
| Performance | Primeiro token do chat stream em ≤ 2s |
| Confiabilidade | Ingestão tolerante a falha: jobs com retry exponencial (3 tentativas) |
| Segurança | Todas as comunicações TLS; OWASP CRS no NGINX; secrets em variáveis de ambiente; nunca logar PII |
| Privacidade | Conteúdo de documento jamais sai do tenant; LLM chama por chunks anonimizados (sem metadados do usuário) |
| Observabilidade | Logs estruturados Serilog; correlation ID por request; health checks ativos |
| Backup | Dump Postgres + sync incremental MinIO para Backblaze B2 semanalmente |
| Operação | Stack inteira sobe com `docker compose up -d` em VM Linux com 2vCPU/4GB |

## 6. Arquitetura de alto nível

Detalhes e diagramas C4 em [`ARCHITECTURE.md`](ARCHITECTURE.md).

Resumo:
- **Camada de borda**: NGINX + ModSecurity expõe HTTPS, faz reverse proxy para `Cirth.Web` e `Cirth.Mcp`.
- **Camada de aplicação**: Blazor Server (`Cirth.Web`) e MCP Server (`Cirth.Mcp`), ambos consumindo a mesma `Cirth.Application`.
- **Camada de domínio**: `Cirth.Domain` puro, sem dependências de infra.
- **Camada de infra**: `Cirth.Infrastructure` implementa portas (EF Core, Qdrant client, MinIO client, LLM adapter).
- **Camada de processamento async**: `Cirth.Worker` consome fila de jobs (tabela `Jobs` no Postgres) e processa ingestão.
- **Dados**: PostgreSQL (relacional + FTS), Qdrant (vetores), Redis (cache), MinIO (objetos).

## 7. Modelo de domínio

Entidades principais:

```
Tenant
 ├─ Users (N) ─ Role
 ├─ Collections (N)
 ├─ Tags (N)
 ├─ Documents (N) ──── DocumentVersions (N)
 │                       └─ Chunks (N)
 ├─ Conversations (N) ── Messages (N)
 ├─ SavedAnswers (N)
 ├─ ApiKeys (N)
 └─ UserQuotas (1 por user)
```

Cada entidade tem:
- `Id` (`Guid`, strongly-typed via record struct)
- `TenantId` (FK obrigatória, global query filter)
- `CreatedAt`, `UpdatedAt`
- Soft delete via `IsDeleted` quando aplicável

## 8. Modelo de dados (PostgreSQL)

Schema principal (resumido):

```sql
CREATE TABLE tenants (
  id UUID PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  entra_object_id UUID NOT NULL UNIQUE,
  email VARCHAR(320) NOT NULL,
  display_name VARCHAR(200) NOT NULL,
  role VARCHAR(20) NOT NULL CHECK (role IN ('Admin','User')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE documents (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  title VARCHAR(500) NOT NULL,
  source_type VARCHAR(30) NOT NULL,
  current_version_id UUID,
  author VARCHAR(200),
  status VARCHAR(20) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE document_versions (
  id UUID PRIMARY KEY,
  document_id UUID NOT NULL REFERENCES documents(id),
  version_number INT NOT NULL,
  content_hash VARCHAR(64) NOT NULL,
  storage_key VARCHAR(500) NOT NULL,
  size_bytes BIGINT NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  is_current BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE chunks (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  document_version_id UUID NOT NULL REFERENCES document_versions(id),
  ordinal INT NOT NULL,
  content TEXT NOT NULL,
  content_tsv TSVECTOR GENERATED ALWAYS AS (to_tsvector('portuguese', content)) STORED,
  token_count INT NOT NULL,
  qdrant_point_id UUID NOT NULL,
  is_current BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE INDEX idx_chunks_tsv ON chunks USING GIN (content_tsv);
CREATE INDEX idx_chunks_tenant_current ON chunks (tenant_id, is_current);

CREATE TABLE tags (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  name VARCHAR(100) NOT NULL,
  color VARCHAR(7),
  UNIQUE (tenant_id, name)
);

CREATE TABLE document_tags (
  document_id UUID NOT NULL,
  tag_id UUID NOT NULL,
  PRIMARY KEY (document_id, tag_id)
);

CREATE TABLE collections (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  name VARCHAR(200) NOT NULL,
  description TEXT
);

CREATE TABLE collection_documents (
  collection_id UUID NOT NULL,
  document_id UUID NOT NULL,
  PRIMARY KEY (collection_id, document_id)
);

CREATE TABLE conversations (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  user_id UUID NOT NULL,
  title VARCHAR(300),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE messages (
  id UUID PRIMARY KEY,
  conversation_id UUID NOT NULL REFERENCES conversations(id),
  role VARCHAR(20) NOT NULL,
  content TEXT NOT NULL,
  cited_chunk_ids UUID[],
  tokens_used INT,
  model VARCHAR(50),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE saved_answers (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  question TEXT NOT NULL,
  answer TEXT NOT NULL,
  cited_chunk_ids UUID[],
  tags UUID[],
  usage_count INT NOT NULL DEFAULT 0,
  utility_score INT NOT NULL DEFAULT 0,
  qdrant_point_id UUID NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE api_keys (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  user_id UUID NOT NULL,
  name VARCHAR(100) NOT NULL,
  key_hash VARCHAR(128) NOT NULL,
  last_used_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ,
  revoked BOOLEAN NOT NULL DEFAULT FALSE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE user_quotas (
  user_id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  daily_token_limit INT NOT NULL DEFAULT 100000,
  daily_upload_limit INT NOT NULL DEFAULT 50,
  storage_limit_bytes BIGINT NOT NULL DEFAULT 5368709120,
  tokens_used_today INT NOT NULL DEFAULT 0,
  uploads_today INT NOT NULL DEFAULT 0,
  storage_used_bytes BIGINT NOT NULL DEFAULT 0,
  reset_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE jobs (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  type VARCHAR(50) NOT NULL,
  payload JSONB NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'Pending',
  attempts INT NOT NULL DEFAULT 0,
  max_attempts INT NOT NULL DEFAULT 3,
  next_run_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  error TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  completed_at TIMESTAMPTZ
);
CREATE INDEX idx_jobs_status_next_run ON jobs (status, next_run_at) WHERE status IN ('Pending','Retrying');
```

## 9. Pipeline de ingestão

```
1. Upload via UI
   ↓
2. Cirth.Web salva arquivo no MinIO (bucket cirth-uploads/{tenant_id}/{doc_id}/v{n})
   ↓
3. Cria Document(status=Pending) + DocumentVersion + Job(type=ProcessDocument)
   ↓
4. Cirth.Worker consome Job:
     a. Download do arquivo do MinIO
     b. Parse (PdfPig para PDF, MarkdownPipeline para MD, AngleSharp para HTML, OpenXML para DOCX)
     c. Chunking (SemanticKernel TextChunker, 800 tokens, overlap 100)
     d. Para cada chunk: embedding via Azure AI Foundry, upsert no Qdrant, insert em chunks
     e. Sugere 3-5 tags via gpt-4.1-mini (job separado)
     f. Atualiza Document.status=Indexed
   ↓
5. Notifica usuário via SignalR hub (toast na UI)
```

## 10. Pipeline de busca híbrida

```
Query do usuário
   ├─→ BM25: SELECT ... FROM chunks WHERE content_tsv @@ plainto_tsquery(...) 
   │         ORDER BY ts_rank_cd LIMIT 50
   │
   └─→ Vector: embed(query) → Qdrant search top 50 (cosine)
   ↓
Reciprocal Rank Fusion (k=60): score = Σ 1 / (k + rank_i)
   ↓
Top K (default 8) merged results
   ↓
Aplicar filtros (tags, collections, date, author)
   ↓
Retornar com highlights (ts_headline do Postgres no trecho)
```

## 11. Pipeline de chat RAG

```
Usuário envia mensagem em conversa C
   ↓
Verifica SavedAnswer: embed(question) → Qdrant search em coleção saved_answers
   Se score ≥ 0.85: oferece ao usuário, espera confirmação
   ↓
Busca híbrida (top 8 chunks)
   ↓
Monta prompt:
   [System] persona Cirth + instruções RAG estritas
   [Context] chunks numerados [1]...[8]
   [History] últimas N mensagens da conversa (sliding window)
   [User] pergunta atual
   ↓
Cirth.Application chama LLM via Microsoft.Extensions.AI (provider Azure AI Foundry)
   ↓
Stream IAsyncEnumerable<string> → SignalR → UI Blazor renderiza token-a-token
   ↓
Ao final: persiste Message(role=Assistant, citations=[...]) e atualiza UserQuota
```

## 12. Servidor MCP

`Cirth.Mcp` é um host ASP.NET Core minimalista que expõe o protocolo MCP via:
- **stdio**: para uso local pelo Claude Desktop
- **HTTP/SSE**: para uso pelo Claude Code remoto

Auth: header `X-Cirth-Api-Key` (no modo HTTP). Stdio: passa key via env var.

Tools internamente despacham para handlers MediatR da `Cirth.Application`, sem duplicar lógica.

## 13. Autenticação e autorização

- OIDC com Entra ID, tenant `c050c98c-b463-4591-ac3b-deb782c0ba6e`, scopes `openid profile email`.
- Cookie httpOnly + secure + SameSite=Lax.
- Claims mapeadas: `oid` → `users.entra_object_id`, `preferred_username` → `email`.
- Primeiro login de um `oid` desconhecido em um tenant novo: cria `Tenant` + `User(Role=Admin)`.
- Convites: Admin gera link `/invite/{token}` com expiração 7 dias.
- Authorization: policies `Admin` e `User`. Page-level `[Authorize(Policy="...")]`.

## 14. Identidade visual

Detalhes em [`DESIGN-SYSTEM.md`](DESIGN-SYSTEM.md). Resumo:
- Tema dark fixo.
- Paleta pergaminho-escuro com ouro de selo e acentos vermelho-runa.
- Tipografia Cinzel (display) + Inter (body) + JetBrains Mono (code).
- MudBlazor com tema customizado Cirth.
- Logo: SVG com runa Cirth (estilo do alfabeto certhas) em ouro sobre fundo escuro.

## 15. Roadmap

### V1 (esta spec)
Texto, busca, chat, MCP, multi-tenant, CI/CD. Critério de pronto: ver seção 16.

### V1.5
- Ingestão de mídia: vídeo (extrair áudio via ffmpeg), áudio (Azure AI Speech batch).
- Pipeline de fila estendido para jobs longos.
- Storage segregado de mídia (bucket `cirth-media`).

### V2
- API REST pública versionada (`/api/v1/...`).
- Observabilidade completa: OTel Collector → Loki/Tempo/Prometheus/Grafana.
- Reranker pós-retrieval (cross-encoder ou LLM-based).
- Semantic chunking.
- Multi-tenancy comercial: billing, planos, isolamento de Qdrant por collection.
- Agentes (multi-turn task execution com tool use).

## 16. Critérios de aceite V1

A V1 está pronta quando:

- [ ] Usuário consegue logar via Entra ID e ser provisionado automaticamente.
- [ ] Usuário consegue fazer upload de PDF, DOCX, MD, TXT, HTML e link web.
- [ ] Após upload, documento aparece como `Indexed` em até 60s (para PDF de até 5MB).
- [ ] Busca retorna resultados relevantes em ≤ 500ms (p95) com highlights.
- [ ] Chat retorna primeiro token em ≤ 2s e completa resposta com citações clicáveis.
- [ ] SavedAnswer sugestão funciona com threshold 0.85.
- [ ] Tags manuais e sugeridas por IA funcionam.
- [ ] Coleções funcionam (criar, adicionar/remover documento, filtrar busca).
- [ ] Versionamento: re-upload cria DocumentVersion e marca chunks antigos.
- [ ] Admin convida User, User aceita, vê próprios documentos.
- [ ] Servidor MCP responde às 6 tools listadas em stdio e HTTP.
- [ ] Quotas bloqueiam usuário ao exceder limite diário com mensagem clara.
- [ ] Rate limiting retorna 429 nos endpoints configurados.
- [ ] Backup roda semanalmente e envia para Backblaze B2 com sucesso.
- [ ] Pipeline CI roda em PR e push, imagem vai para GHCR em merge para main.
- [ ] Deploy via webhook na VM funciona.
- [ ] Health checks reportam Healthy em todos os checks com infra OK.
- [ ] Logs estruturados Serilog rodando, com correlation ID por request.
- [ ] Tema Cirth aplicado em todas as páginas, fontes carregando, paleta consistente.
- [ ] ModSecurity bloqueia ataques OWASP Top 10 básicos.
- [ ] Documentação: README, SPEC, ARCHITECTURE, DESIGN-SYSTEM, 6 ADRs.
