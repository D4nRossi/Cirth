# Cirth — Status atual & retomada

> Atualizado em **2026-05-13**. Arquivo vivo: atualize ao terminar uma sessão pra a próxima começar sem perder contexto.

---

## 1. Estado hoje (snapshot)

- **Branch ativa**: `main` (limpa, sem commits pendentes)
- **Último commit**: `91a4362` — *"docs: sweep dos docs após migração Blazor → Razor Pages + dúzia de fixes"*
- **Build**: Release 0/0 erros/warnings em toda a solution (`dotnet build src/Cirth.sln -c Release`)
- **Testes**: 18 Domain + 6 Application passando (`make test`)
- **DB**: migrations todas aplicadas — última = `AddContentTsvGenerated` (BM25 column + GIN)

### Stack consolidada

| Camada | O que está rodando |
|---|---|
| Web | Razor Pages + HTMX, CSS custom em `wwwroot/css/cirth.css`, sem framework UI |
| IA — Chat | OpenAI SDK puro contra `https://danie-mc4ryviy-westeurope.openai.azure.com/openai/v1` (Responses API). Deployment: `gpt-4.1` |
| IA — Embedding | Azure.AI.OpenAI contra `https://danie-mc4ryviy-westeurope.cognitiveservices.azure.com/`. Deployment: `text-embedding-ada-002` (1536 dim) |
| Persistência | PostgreSQL 16 (com `content_tsv` GENERATED + GIN), Qdrant (Cosine, multi-tenant filter), Redis, MinIO |
| Worker | `JobPollingService` (a cada 5s) + `StuckJobRecoveryService` (a cada 2min, threshold 10min) |
| Auth | Entra ID OIDC single-tenant. ClientId em user-secrets do `Cirth.Web` |
| Logs | Serilog Console + arquivo rolling diário em `src/Cirth.{Web,Worker}/logs/`. Viewer em `/Admin/Logs` |

### O que está testado e funciona

- Login via Entra ID, redirect correto pós-login
- `/Admin/Conexões`: 6 cards verdes (PostgreSQL, Redis, Qdrant, MinIO, Azure AI — Chat, Azure AI — Embeddings) — probes reais não-persistentes
- `/Documents/Upload` (página dedicada, não dialog): arquivo + URL, com tag picker inline funcionando
- Chat RAG com sequência `🔍 Analisando → 📚 Buscando → 🤖 Gerando` antes do streaming dos tokens
- Botão "☆ Salvar resposta" no fim de cada bubble do assistant → registra em `SavedAnswers` + embeda pergunta no Qdrant
- `/Admin/Logs` com filtro por origem (web/worker), nível mínimo, substring, auto-refresh 5s
- Recovery automático de jobs travados em Processing > 10min
- Botão "Reprocessar falhas" em `/Admin/Conexões` re-enfileira tudo em status Failed

---

## 2. Como retomar (no Linux)

```bash
cd ~/projects/KM-Cirth/cirth
git pull origin main                          # caso tenha algo novo

# Infra
docker compose -f docker-compose.infra.yml up -d
docker ps | grep cirth                        # confirmar Postgres/Redis/Qdrant/MinIO up

# Web + Worker em terminais separados
make watch                                    # https://localhost:5001
make worker                                   # processa fila de jobs

# (opcional) tail dos logs em outro terminal
make logs-web
make logs-worker
```

Se voltar e o user-secrets sumiram (não deveria — ficam em `~/.microsoft/usersecrets/`), os comandos para repopular estão em `RUN.md` seção 1.

---

## 3. TODO mapeado — próximas sessões

### 3.1 IA dinâmica via UI (feature, pedido do usuário)

**Objetivo**: trocar LLM e embedding sem recompilar/restartar. Tudo via tela `/Admin/AI`.

**Esboço do plano**:

1. **Domain**: entidade `AiModelSetting` (tenant-global ou single-instance — decidir) com:
   - `ChatProvider`, `ChatEndpoint`, `ChatDeployment`, `ChatApiKey` (encriptado via `IDataProtectionProvider`)
   - `EmbeddingProvider`, `EmbeddingEndpoint`, `EmbeddingDeployment`, `EmbeddingDimensions`
   - `IsActive`, audit fields
2. **Repository + cache em memória** (invalidate on write).
3. **DI muda de Singleton → Factory**:
   - `IChatClient` e `IEmbeddingGenerator` resolvidos via `IAiClientFactory.GetChatClient()` / `.GetEmbeddingGenerator()` que consulta o repository.
   - Permite trocar sem restart do processo.
4. **Página `/Admin/AI`**: form pra editar settings + botão "Validar" que roda probe (igual o health check) ANTES de salvar.
5. **Migração de embedding dimensão**: se o usuário trocar pra modelo com dimensão diferente, marcar todos os chunks como `needs_reembedding`, criar nova coleção Qdrant, worker re-embedar em batches, drop old collection ao final. Isso é grande — provavelmente fica fora dessa primeira iteração.

**Onde começar**: criar a entidade no Domain primeiro, depois o repository, depois a factory. Página vem por último.

### 3.2 Reprocessar BM25 de docs antigos

A coluna `content_tsv` é GENERATED STORED — Postgres preenche automaticamente em INSERTs/UPDATEs. Documentos inseridos ANTES da migration `AddContentTsvGenerated` (commit `7d47d9b`) deveriam ter `content_tsv` computado automaticamente quando a coluna foi criada (porque é STORED, não VIRTUAL).

**Verificar**:
```sql
-- conectar via docker exec cirth-postgres psql -U cirth -d cirth
SELECT id, content_tsv IS NOT NULL AS indexed FROM chunks LIMIT 5;
```

Se algum row tiver `indexed=false`, rodar:
```sql
UPDATE chunks SET content = content;  -- triggers re-compute do generated column
```

Mas isso provavelmente é desnecessário porque STORED é computado uma vez ao criar a coluna. Confirmar antes de mexer.

### 3.3 Saved Answers — UI mínima

Hoje `/Saved` lista mas não tem operações. Faltam:
- [ ] Botão "Deletar" em cada resposta salva (com confirmação)
- [ ] Visualização individual em `/Saved/{id}` mostrando pergunta + resposta + chunks citados + score de utilidade
- [ ] (Talvez) "Editar pergunta" — útil pra ajustar phrasing que afeta a similaridade do SavedAnswer lookup

### 3.4 Observabilidade — pequenos polimentos

- [ ] Página `/Admin/Logs` hoje lê o arquivo inteiro e dá `.Reverse()`. Pra logs grandes (>10MB) vai ficar lento. Trocar por seek-based tail.
- [ ] `/metrics` está exposto mas sem Prometheus scrape configurado. Quando for pra produção, configurar.

### 3.5 Itens que apareceram nos diagnósticos mas não viraram bug

- **MCP**: `src/Cirth.Mcp` existe mas nunca foi exercitado nessa sessão. Provavelmente funciona (compila), mas vai precisar de smoke test quando for usar via Claude Desktop.
- **Backup script**: `nginx/` e configuração de produção (Cloudflare Tunnel, backup pra B2) tudo descrito no `RUN.md` mas não testado end-to-end ainda.

---

## 4. Bugs resolvidos na última sessão (referência pra não regredir)

Cronologia dos commits (mais recente primeiro):

| Commit | Bug | Causa-raiz |
|---|---|---|
| `91a4362` | (docs) | — |
| `75e86b8` | Chat sem progress messages + sem save answer | Kestrel HTTP/2 buffera primeiro frame; sem UI pra criar SavedAnswer |
| `ac97a22` | Chat 500 EF: ".Where(m => m.ConversationId.Value == @id) could not be translated" | EF Core 10 não traduz `.Value` em LINQ predicate mesmo com HasConversion |
| `bbe477e` | "Headers are read-only" + LINQ TakeLast | OnGetAsync com `Task` faz Razor Pages aplicar PageResult implícito após SSE; TakeLast não traduz |
| `e68fedd` | Logs não apareciam no `make watch` | Cirth.Web/appsettings.json faltava `Serilog.WriteTo` — sinks zerados após ReadFromConfiguration |
| `7d47d9b` | Chat: "column content_tsv does not exist" | InitialCreate nunca criou a coluna pro Bm25SearchService |
| `5162661` | Chat 404 DeploymentNotFound + jobs travados | Foundry mudou pra Responses API `/openai/v1`; sem recovery de Processing órfão |
| `7c2f286` | POSTs retornando 400 (criar tag, etc) | `<body hx-headers='{"RequestVerificationToken": ""}'>` com valor VAZIO |
| `e866072` | Postgres "42703 column s.Value does not exist", Redis "INFO admin mode", Admin tabs duplicadas | SqlQueryRaw<string> exige coluna "Value"; InfoAsync é admin-only; HTMX 2.x auto-processa `data-hx-*` |
| `180b487` | (refactor) Migração Blazor → Razor Pages + HTMX | — |

**Regras a observar** (todas documentadas em `CLAUDE.md`):
- EF Core: nunca `.Value ==` em LINQ predicate, nunca `TakeLast`, nunca `enum.ToString().ToLower()` em projection, `SqlQueryRaw<string>` exige alias `"Value"`.
- Razor Pages: handlers SSE/streaming devem retornar `Task<IActionResult>` terminando em `EmptyResult` (não `Task` puro).
- HTMX: nunca `data-hx-*` em metadados de JS custom (auto-processado); antiforgery token sempre vem do `_Layout` injetado em `hx-headers`.
- Serilog: TODO appsettings.json precisa de `WriteTo` explícito — `ReadFromConfiguration` zera sinks do bootstrap.

---

## 5. Quando voltar amanhã, ordem sugerida

1. `git pull` (caso tenha mexido algo do Windows — improvável, mas previne surpresa)
2. Subir infra + watch + worker
3. Abrir `/Admin/Logs` no browser num tab, deixar aberto
4. Decidir entre TODO 3.1 (IA dinâmica) vs algo menor (3.3 saved answers) pra warm-up
5. Atualizar este arquivo no fim da sessão com novo `STATUS.md` snapshot
